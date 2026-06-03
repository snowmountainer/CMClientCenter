using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.PowerShell.Engine;
using CMClientCenter.PowerShell.Helpers;
using CMClientCenter.Shared.Enums;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.PowerShell.Executors;

public class CMAgentExecutor(RunspaceManager runspace, ILogger<CMAgentExecutor> logger)
    : IExecutorService<CMAgentInfo>
{
    public async Task<Result<CMAgentInfo>> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            var results = await runspace.InvokeAsync(
                EmbeddedScripts.Load("Get-CMAgentStatus.ps1"), ct);

            if (results.Count == 0)
                return Result<CMAgentInfo>.Failure("Kein Ergebnis vom Script");

            var obj = results[0];

            // DiagInfo für Debugging loggen
            var diagInfo = PSObjectMapper.GetString(obj, "DiagInfo");
            if (!string.IsNullOrEmpty(diagInfo))
                logger.LogInformation("CCM Agent DiagInfo: {DiagInfo}", diagInfo);

            var clientVersion = PSObjectMapper.GetString(obj, "ClientVersion");
            var clientState   = PSObjectMapper.GetString(obj, "ClientState");
            var isEnabled     = PSObjectMapper.GetBool(obj, "IsEnabled");

            // ClientState aus Version ableiten wenn leer
            if (string.IsNullOrEmpty(clientState))
                clientState = string.IsNullOrEmpty(clientVersion) ? "NotInstalled" : "Healthy";

            return Result<CMAgentInfo>.Success(new CMAgentInfo(
                ClientVersion:         clientVersion,
                ClientId:              PSObjectMapper.GetString(obj, "ClientId"),
                State:                 ParseClientState(clientState),
                IsEnabled:             isEnabled,
                LastHardwareInventory: PSObjectMapper.GetDateTime(obj, "LastHWInventory"),
                LastSoftwareInventory: PSObjectMapper.GetDateTime(obj, "LastSWInventory"),
                LastPolicyRequest:     PSObjectMapper.GetDateTime(obj, "LastPolicyRequest"),
                SiteCode:              PSObjectMapper.GetString(obj, "SiteCode"),
                ManagementPoint:       PSObjectMapper.GetString(obj, "ManagementPoint"),
                CacheSize:   PSObjectMapper.GetString(obj, "CacheSize"),
                DiagInfo:    PSObjectMapper.GetString(obj, "DiagInfo")
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get CM Agent info");
            return Result<CMAgentInfo>.Failure(ex.Message, ex);
        }
    }

    private static CMClientState ParseClientState(string state) => state.ToLower() switch
    {
        "healthy"      => CMClientState.Healthy,
        "warning"      => CMClientState.Warning,
        "error"        => CMClientState.Error,
        "notinstalled" => CMClientState.NotInstalled,
        _              => CMClientState.Unknown
    };
}
