namespace InstalledSoftwareInventory.Models;

/// <summary>
/// A single installed program, as reported by the Windows "uninstall" registry
/// keys. Only non-sensitive metadata is captured.
/// </summary>
public sealed record InstalledApp
{
    public string Name { get; init; } = string.Empty;
    public string? Version { get; init; }
    public string? Publisher { get; init; }
    public DateOnly? InstallDate { get; init; }
    public string? InstallLocation { get; init; }

    /// <summary>Which registry hive the entry was found in (HKLM 64-bit, HKLM 32-bit, HKCU).</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>True if a process is currently running from <see cref="InstallLocation"/>.</summary>
    public bool IsRunning { get; init; }
}
