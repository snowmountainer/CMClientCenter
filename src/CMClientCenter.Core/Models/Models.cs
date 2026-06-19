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
    List<CCMApplication> Applications,
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

public record AppSettings
{
    public AppTheme Theme { get; init; } = AppTheme.System;
}

