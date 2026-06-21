using CMClientCenter.Shared.Enums;

namespace CMClientCenter.Core.Models;

public record TargetComputer(string Hostname)
{
    public ConnectionMode ConnectionMode { get; init; } = ConnectionMode.AutoDetect;
    public string? Username { get; init; }
    public bool IsLocal =>
        Hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        Hostname == "127.0.0.1" ||
        Hostname.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
}

public record ConnectionResult(
    bool IsConnected,
    ConnectionMode Mode,
    string? ErrorMessage = null,
    string? OSVersion = null,
    string? PSVersion = null
);

public record CMAgentInfo(
    string ClientVersion,
    string ClientId,
    CMClientState State,
    bool IsEnabled,
    DateTime? LastHardwareInventory,
    DateTime? LastSoftwareInventory,
    DateTime? LastPolicyRequest,
    string SiteCode,
    string ManagementPoint,
    string CacheSize,
    string DiagInfo = ""
);

public record HardwareInfo(
    // System
    string Manufacturer,
    string Model,
    string SerialNumber,
    string BIOSVersion,
    string BIOSDate,
    // CPU
    string CPUName,
    int CPUCores,
    int CPULogical,
    string CPUSocket,
    int CPUMaxMHz,
    // RAM
    int TotalRAMGB,
    List<RAMSlot> RAMSlots,
    // GPU
    string GPUName,
    int GPUVRAMMB,
    // OS
    string OSCaption,
    string OSBuild,
    string OSArch,
    string OSInstall,
    string LastBoot,
    // Storage + Network
    List<DiskInfo> Disks,
    List<NICInfo> NICs
);

public record DiskInfo(
    string DriveLetter,
    string Label,
    double TotalGB,
    double FreeGB,
    int FreePct,
    string FileSystem
);

public record RAMSlot(
    string Slot,
    int SizeGB,
    string SpeedMHz,
    string Manufacturer
);

public record NICInfo(
    string Description,
    string IPAddress,
    string MACAddress
);

public record SoftwareItem(
    string Name,
    string Version,
    string Publisher,
    DateTime? InstallDate)
{
    public string InstallDateDisplay =>
        InstallDate.HasValue ? InstallDate.Value.ToString("dd.MM.yyyy") : "";
}

public record HealthCheck(
    string Category,
    string Name,
    string Status,
    string Value,
    string Detail = ""
);

public record CMAction(
    string Name,
    CMActionType ActionType,
    string Description
)
{
    public static IReadOnlyList<CMAction> AllActions =>
    [
        new("Machine Policy Retrieval",    CMActionType.MachinePolicy,           "Retrieves machine policies from the MP"),
        new("Discovery Data Collection",   CMActionType.DiscoveryDataCollection,  "Sends discovery data to the site server"),
        new("Software Inventory",          CMActionType.SoftwareInventory,        "Runs a software inventory scan"),
        new("Hardware Inventory",          CMActionType.HardwareInventory,        "Runs a hardware inventory scan"),
        new("Software Updates Deployment", CMActionType.UpdateDeployment,         "Checks for and installs updates"),
        new("Software Updates Scan",       CMActionType.UpdateScan,               "Scans for available updates"),
        new("Application Deployment",      CMActionType.ApplicationDeployment,    "Evaluates application deployments"),
    ];
}

public record LogEntry(
    string Time,
    string Component,
    string Severity,   // Info | Warning | Error
    string Message
);

public record LogFileInfo(
    string Name,
    int SizeKB,
    string Modified,
    string Folder
);

public record CCMToolsInfo(
    int CacheSizeMB,
    int CacheUsedMB,
    int CacheFreeMB,
    string CachePath,
    List<CacheItem> CacheItems,
    bool RebootPending,
    List<string> RebootSources,
    bool CCMSetupRunning
);

public record CacheItem(
    string ContentId,
    string ContentVersion,
    string Location,
    double SizeMB,
    string LastRefTime
);

public record CCMApplication(
    string Id,
    string Revision,
    string Name,
    string Publisher,
    string SoftwareVersion,
    string InstallState,
    string ResolvedState
);

// Software Center: "Operating Systems" — Task Sequences (inkl. OSD/Bare-Metal),
// gelesen aus CCM_Program (ROOT\ccm\clientsdk) gefiltert auf TaskSequence=true.
// CCM_TaskSequence existiert nicht auf allen Clients und wurde verworfen
// (siehe Get-CCMTaskSequences.ps1 fuer Details).
//
// HighImpact-Felder kommen direkt aus ConfigMgr (vom Admin in der Console
// gepflegt) und werden 1:1 fuer den Bestaetigungsdialog vor dem Ausfuehren
// verwendet, statt einen eigenen Warntext zu erfinden.
public record CCMTaskSequence(
    string ProgramId,
    string PackageId,
    string Name,
    string FullName,
    string PackageName,
    string Description,
    string Publisher,
    string Version,
    bool HighImpact,
    bool HighImpactTaskSequence,
    bool CustomHighImpactSet,
    string CustomHighImpactHeadline,
    string CustomHighImpactWarningTop,
    string CustomHighImpactWarning,
    string CustomHighImpactWarningInstall,
    int EvaluationState,
    string LastRunStatus,
    string LastRunTime,
    bool RestartRequired,
    bool AdvertisedDirectly,
    bool Published
);

// "Updates" Page — "All Updates" / "Pending Updates".
//
// Anzeige-Quelle: CCM_UpdateStatus (root\ccm\SoftwareUpdates\UpdatesStore).
// Diese Klasse liefert Status="Installed"/"Missing" direkt (keine Compliance-
// State-Zahl zu interpretieren), deckt sich mit dem, was das alte "Client
// Center for Configuration Manager"-Tool unter "All Updates" zeigt. Sie
// liefert aber KEINE UpdateID und keine Install-Methode.
//
// Installations-Quelle: CCM_SoftwareUpdate (ROOT\ccm\clientsdk) — getrennte
// Klasse, nur dort existiert eine UpdateID, die CCM_SoftwareUpdatesManager.
// InstallUpdates (Array-Parameter!) zum Anstossen der Installation benötigt.
//
// Get-CCMSoftwareUpdates.ps1 macht den Title/Article-Abgleich bereits in
// PowerShell und liefert InstallableUpdateId nur, wenn ein Match in
// CCM_SoftwareUpdate gefunden wurde — leer/null bedeutet: in der UI nicht
// installierbar (Button deaktiviert), z.B. weil der Client das Update noch
// nicht als "deployed and applicable" erkannt hat.
public record CCMSoftwareUpdate(
    string UniqueId,
    string Article,
    string Bulletin,
    string Title,
    string Status,            // "Installed" | "Missing"
    int RevisionNumber,
    string ScanTime,
    string UpdateClassification,
    string? InstallableUpdateId   // UpdateID aus CCM_SoftwareUpdate, falls Match gefunden — sonst null
);

public record AppSettings
{
    public AppTheme Theme { get; init; } = AppTheme.System;
}


