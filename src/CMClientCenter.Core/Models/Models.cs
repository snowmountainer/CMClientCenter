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
    string Description,
    ActionCategory Category = ActionCategory.Standard
)
{
    public static IReadOnlyList<CMAction> AllActions =>
    [
        // ── Standard (entspricht der klassischen ConfigMgr-Systemsteuerung "Actions"-Seite) ──
        new("Hardware Inventory Cycle",                    CMActionType.HardwareInventory,       "Runs a hardware inventory scan"),
        new("Software Inventory Cycle",                    CMActionType.SoftwareInventory,       "Runs a software inventory scan"),
        new("Discovery Data Collection Cycle",              CMActionType.DiscoveryDataCollection, "Sends discovery data to the site server"),
        new("File Collection Cycle",                        CMActionType.FileCollection,          "Collects configured files from the client"),
        new("Machine Policy Retrieval Cycle",                CMActionType.MachinePolicy,           "Retrieves machine policies from the MP"),
        new("Machine Policy Evaluation Cycle",               CMActionType.MachinePolicyEval,       "Re-evaluates already retrieved machine policies"),
        new("User Policy Retrieval Cycle",                   CMActionType.UserPolicyRequest,       "Retrieves user policies from the MP"),
        new("User Policy Evaluation Cycle",                  CMActionType.UserPolicyEval,          "Re-evaluates already retrieved user policies"),
        new("Software Metering Usage Report Cycle",          CMActionType.SoftwareMeteringReport,  "Sends software metering usage data"),
        new("Windows Installer Source List Update Cycle",    CMActionType.SourceUpdate,            "Updates MSI source list locations"),
        new("Software Updates Deployment Evaluation Cycle",  CMActionType.UpdateDeployment,         "Checks for and installs updates"),
        new("Software Updates Scan Cycle",                   CMActionType.UpdateScan,               "Scans for available updates"),
        new("Application Deployment Evaluation Cycle",       CMActionType.ApplicationDeployment,    "Evaluates application deployments"),

        // ── Erweitert (seltener benötigt, primär Troubleshooting) ──
        new("Software Updates Install Cycle (SUM)",          CMActionType.SumUpdatesInstall,             "Triggers install of already-scanned updates", ActionCategory.Advanced),
        new("DCM Policy",                                    CMActionType.DcmPolicy,                     "Re-evaluates Desired Configuration Management policy", ActionCategory.Advanced),
        new("Send Unsent State Messages",                    CMActionType.SendUnsentStateMessage,        "Flushes queued state messages to the MP", ActionCategory.Advanced),
        new("State System Policy Cache Cleanout",            CMActionType.StateSystemPolicyCacheCleanout, "Cleans the state system policy cache", ActionCategory.Advanced),
        new("Update Store Policy",                           CMActionType.UpdateStorePolicy,             "Updates the update store policy", ActionCategory.Advanced),
        new("State System Bulk Send (High)",                 CMActionType.StateSystemBulkSendHigh,       "Forces high-priority bulk state message send", ActionCategory.Advanced),
        new("State System Bulk Send (Low)",                  CMActionType.StateSystemBulkSendLow,        "Forces low-priority bulk state message send", ActionCategory.Advanced),
        new("Application Manager User Policy Action",        CMActionType.ApplicationUserPolicyAction,   "Re-evaluates user-targeted app deployments", ActionCategory.Advanced),
        new("Application Manager Global Evaluation",         CMActionType.ApplicationGlobalEvaluation,   "Full re-evaluation of all app deployments", ActionCategory.Advanced),
        new("Power Management Start Summarizer",             CMActionType.PowerManagementSummarizer,     "Starts power management data summarization", ActionCategory.Advanced),
        new("Endpoint Protection Deployment Reevaluate",     CMActionType.EndpointDeploymentReevaluate,  "Re-evaluates Endpoint Protection deployment", ActionCategory.Advanced),
        new("Endpoint AM Policy Reevaluate",                 CMActionType.EndpointAMPolicyReevaluate,    "Re-evaluates Endpoint Protection AM policy", ActionCategory.Advanced),
        new("External Event Detection",                      CMActionType.ExternalEventDetection,        "Triggers external event detection", ActionCategory.Advanced),
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
    string Folder,
    string Source = "CCM"   // CCM | CCMSetup | PSADT
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

    // Folder scanned by the "Console" page for user-supplied .ps1 scripts
    // ("Run PS" in the old Client Center for Configuration Manager).
    // Empty/null means: use AppSettingsService.DefaultScriptsFolder.
    public string? ScriptsFolder { get; init; }

    // Width (in pixels) of the "Script Output" panel on the Console page,
    // set by dragging OutputSplitter. Null means: use the default (420px)
    // hardcoded in ConsolePage.xaml — only written once the user actually
    // drags the splitter, so a fresh install still gets the XAML default
    // without this needing a matching default value here.
    public double? ConsoleOutputColumnWidth { get; init; }

    // Last-used main window position/size, in physical pixels (AppWindow's
    // native unit). Null means: no saved geometry yet — MainWindow keeps
    // its XAML/SDK default size and lets Windows pick the initial position.
    // Always holds the RESTORED (non-maximized, non-minimized) geometry,
    // even if the window was maximized when the app closed — that way
    // toggling out of "maximized" on next launch lands somewhere sensible.
    public int? WindowX { get; init; }
    public int? WindowY { get; init; }
    public int? WindowWidth { get; init; }
    public int? WindowHeight { get; init; }
    public bool WindowIsMaximized { get; init; }
}

// A .ps1 file discovered for the "Console" page's "Run PS" list — either a
// built-in script shipped with the app (PSScripts\ next to the .exe,
// originally from "Client Center for Configuration Manager", read-only by
// convention) or a user-supplied one from AppSettings.ScriptsFolder.
// Subfolders are scanned recursively in both locations, mirroring the old
// tool's grouped script list.
public record CustomScriptInfo(
    string Name,
    string FullPath,
    DateTime LastModified,
    string GroupName,   // relative subfolder path, e.g. "DO\Reboot" — "(Root)" for top-level scripts
    bool IsBuiltin       // true = from PSScripts\ (shipped, read-only), false = from the user's ScriptsFolder
);


