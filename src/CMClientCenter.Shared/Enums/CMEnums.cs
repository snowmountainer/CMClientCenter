namespace CMClientCenter.Shared.Enums;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public enum CMClientState
{
    Unknown,
    Healthy,
    Warning,
    Error,
    NotInstalled
}

public enum CMActionType
{
    MachinePolicy          = 1,
    DiscoveryDataCollection = 2,
    SoftwareInventory      = 3,
    HardwareInventory      = 4,
    UpdateDeployment       = 5,
    UpdateScan             = 6,
    SoftwareMeteringReport = 7,
    SourceUpdate           = 8,
    FileCollection         = 9,
    ApplicationDeployment  = 10,

    // ── Standard-Ergänzungen ──
    MachinePolicyEval      = 11,
    UserPolicyRequest      = 12,
    UserPolicyEval         = 13,

    // ── Erweitert / Troubleshooting ──
    SumUpdatesInstall              = 20,
    DcmPolicy                      = 21,
    SendUnsentStateMessage         = 22,
    StateSystemPolicyCacheCleanout = 23,
    UpdateStorePolicy              = 24,
    StateSystemBulkSendHigh        = 25,
    StateSystemBulkSendLow         = 26,
    ApplicationUserPolicyAction    = 27,
    ApplicationGlobalEvaluation    = 28,
    PowerManagementSummarizer      = 29,
    EndpointDeploymentReevaluate   = 30,
    EndpointAMPolicyReevaluate     = 31,
    ExternalEventDetection         = 32
}

public enum ActionCategory
{
    Standard,
    Advanced
}
