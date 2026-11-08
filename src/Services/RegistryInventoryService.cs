using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using InstalledSoftwareInventory.Models;

namespace InstalledSoftwareInventory.Services;

/// <summary>
/// Reads the standard Windows "uninstall" registry keys to build a read-only
/// inventory of installed desktop software. Nothing is launched or modified;
/// the service only reads metadata that Windows itself exposes in Add/Remove
/// Programs.
/// </summary>
public sealed class RegistryInventoryService
{
    private const string UninstallPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string UninstallPathWow =
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public IReadOnlyList<InstalledApp> GetInstalledApps()
    {
        var apps = new List<InstalledApp>();
        var runningPaths = GetRunningProcessDirectories();

        ReadHive(Registry.LocalMachine, UninstallPath, "HKLM (64-bit)", apps, runningPaths);
        ReadHive(Registry.LocalMachine, UninstallPathWow, "HKLM (32-bit)", apps, runningPaths);
        ReadHive(Registry.CurrentUser, UninstallPath, "HKCU", apps, runningPaths);

        return apps
            .GroupBy(a => (a.Name, a.Version))      // de-dupe entries listed in several hives
            .Select(g => g.First())
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void ReadHive(
        RegistryKey hive,
        string path,
        string source,
        List<InstalledApp> sink,
        HashSet<string> runningPaths)
    {
        using var key = hive.OpenSubKey(path);
        if (key is null)
            return;

        foreach (var subName in key.GetSubKeyNames())
        {
            using var sub = key.OpenSubKey(subName);
            if (sub is null)
                continue;

            var name = sub.GetValue("DisplayName") as string;
            if (string.IsNullOrWhiteSpace(name))
                continue;                            // skip system components / patches

            // Hide update entries, which Add/Remove Programs also hides.
            if ((sub.GetValue("SystemComponent") as int?) == 1)
                continue;

            var location = (sub.GetValue("InstallLocation") as string)?.Trim();

            sink.Add(new InstalledApp
            {
                Name = name.Trim(),
                Version = sub.GetValue("DisplayVersion") as string,
                Publisher = sub.GetValue("Publisher") as string,
                InstallDate = ParseInstallDate(sub.GetValue("InstallDate") as string),
                InstallLocation = location,
                Source = source,
                IsRunning = !string.IsNullOrEmpty(location)
                            && runningPaths.Contains(NormalizeDir(location)),
            });
        }
    }

    /// <summary>InstallDate is stored as "yyyyMMdd"; parse it defensively.</summary>
    private static DateOnly? ParseInstallDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateOnly.TryParseExact(
            raw.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    /// <summary>
    /// Directories that currently host a running process, so the UI can show a
    /// simple "running" flag. Processes we cannot inspect (access denied) are
    /// skipped silently.
    /// </summary>
    private static HashSet<string> GetRunningProcessDirectories()
    {
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var file = p.MainModule?.FileName;
                var dir = string.IsNullOrEmpty(file) ? null : Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir))
                    dirs.Add(NormalizeDir(dir));
            }
            catch
            {
                // Protected/system processes throw on MainModule; ignore them.
            }
            finally
            {
                p.Dispose();
            }
        }
        return dirs;
    }

    private static string NormalizeDir(string dir) =>
        dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
