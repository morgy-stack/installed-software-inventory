using System.IO;
using System.Text;
using InstalledSoftwareInventory.Models;

namespace InstalledSoftwareInventory.Services;

/// <summary>Writes an inventory to a UTF-8 CSV file (Excel-friendly).</summary>
public sealed class CsvExportService
{
    public void Export(IEnumerable<InstalledApp> apps, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Version,Publisher,InstallDate,InstallLocation,Source,IsRunning");

        foreach (var a in apps)
        {
            sb.Append(Escape(a.Name)).Append(',')
              .Append(Escape(a.Version)).Append(',')
              .Append(Escape(a.Publisher)).Append(',')
              .Append(Escape(a.InstallDate?.ToString("yyyy-MM-dd"))).Append(',')
              .Append(Escape(a.InstallLocation)).Append(',')
              .Append(Escape(a.Source)).Append(',')
              .Append(a.IsRunning ? "yes" : "no")
              .AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>RFC-4180 style quoting for fields containing commas, quotes or newlines.</summary>
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
