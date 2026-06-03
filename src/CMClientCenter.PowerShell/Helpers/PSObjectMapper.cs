using System.Management.Automation;
using System.Globalization;

namespace CMClientCenter.PowerShell.Helpers;

public static class PSObjectMapper
{
    /// <summary>Unwrapped den Wert aus einem PSObject falls nötig.</summary>
    private static object? Unwrap(object? val) =>
        val is PSObject pso ? pso.BaseObject : val;

    public static string GetString(PSObject obj, string propertyName)
    {
        var val = Unwrap(obj.Properties[propertyName]?.Value);
        return val?.ToString() ?? string.Empty;
    }

    public static int GetInt(PSObject obj, string propertyName)
    {
        var val = Unwrap(obj.Properties[propertyName]?.Value);
        return int.TryParse(val?.ToString(), out var v) ? v : 0;
    }

    public static long GetLong(PSObject obj, string propertyName)
    {
        var val = Unwrap(obj.Properties[propertyName]?.Value);
        return long.TryParse(val?.ToString(), out var v) ? v : 0;
    }

    public static bool GetBool(PSObject obj, string propertyName)
    {
        var val = Unwrap(obj.Properties[propertyName]?.Value);
        return val switch
        {
            bool b   => b,
            int i    => i != 0,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
            _        => false
        };
    }

    public static DateTime? GetDateTime(PSObject obj, string propertyName)
    {
        var val = Unwrap(obj.Properties[propertyName]?.Value);
        if (val is null) return null;

        // Direkt DateTime
        if (val is DateTime dt) return dt;

        var s = val.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;

        // yyyyMMdd (Registry InstallDate)
        if (s.Length == 8 && DateTime.TryParseExact(
                s, "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dtReg))
            return dtReg;

        // Allgemeines Datum
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dtGen))
            return dtGen;

        return null;
    }

    public static DateTime? ParseDMTFDateTime(string? dmtf)
    {
        if (string.IsNullOrEmpty(dmtf)) return null;
        try
        {
            return new DateTime(
                int.Parse(dmtf[0..4]),  int.Parse(dmtf[4..6]),
                int.Parse(dmtf[6..8]),  int.Parse(dmtf[8..10]),
                int.Parse(dmtf[10..12]),int.Parse(dmtf[12..14]),
                DateTimeKind.Local);
        }
        catch { return null; }
    }
}
