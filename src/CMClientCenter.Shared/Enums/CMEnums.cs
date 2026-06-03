namespace CMClientCenter.Shared.Enums;

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
    ApplicationDeployment  = 10
}
