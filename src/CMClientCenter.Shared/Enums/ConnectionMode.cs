namespace CMClientCenter.Shared.Enums;

public enum ConnectionMode
{
    /// <summary>Automatisch erkennen (localhost vs. Remote)</summary>
    AutoDetect,
    /// <summary>Local execution without WinRM</summary>
    Local,
    /// <summary>Remote via WinRM / PSSession</summary>
    Remote
}
