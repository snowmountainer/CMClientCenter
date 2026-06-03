namespace CMClientCenter.Shared.Enums;

public enum ConnectionMode
{
    /// <summary>Automatisch erkennen (localhost vs. Remote)</summary>
    AutoDetect,
    /// <summary>Lokale Ausführung ohne WinRM</summary>
    Local,
    /// <summary>Remote via WinRM / PSSession</summary>
    Remote
}
