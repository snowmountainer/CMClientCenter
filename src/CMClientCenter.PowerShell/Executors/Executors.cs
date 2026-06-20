using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.PowerShell.Engine;
using CMClientCenter.PowerShell.Helpers;
using CMClientCenter.Shared.Enums;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.PowerShell.Executors;

// ─── Hardware ─────────────────────────────────────────────────────────────

public class HardwareExecutor(RunspaceManager runspace, ILogger<HardwareExecutor> logger)
    : IExecutorService<HardwareInfo>
{
    public async Task<Result<HardwareInfo>> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            var results = await runspace.InvokeAsync(
                EmbeddedScripts.Load("Get-HardwareInfo.ps1"), ct);

            if (results.Count == 0)
                return Result<HardwareInfo>.Failure("No hardware data received");

            var obj = results[0];

            // RAM Slots
            var ramSlots = new List<RAMSlot>();
            if (obj.Properties["RAMSlots"]?.Value is System.Collections.IEnumerable ramObjects)
                foreach (var r in ramObjects)
                    if (r is System.Management.Automation.PSObject rp)
                        ramSlots.Add(new RAMSlot(
                            Slot:         PSObjectMapper.GetString(rp, "Slot"),
                            SizeGB:       PSObjectMapper.GetInt(rp, "SizeGB"),
                            SpeedMHz:     PSObjectMapper.GetString(rp, "SpeedMHz"),
                            Manufacturer: PSObjectMapper.GetString(rp, "Manufacturer")));

            // Disks
            var disks = new List<DiskInfo>();
            if (obj.Properties["Disks"]?.Value is System.Collections.IEnumerable diskObjects)
                foreach (var d in diskObjects)
                    if (d is System.Management.Automation.PSObject dp)
                        disks.Add(new DiskInfo(
                            DriveLetter: PSObjectMapper.GetString(dp, "DriveLetter"),
                            Label:       PSObjectMapper.GetString(dp, "Label"),
                            TotalGB:     PSObjectMapper.GetLong(dp, "TotalGB"),
                            FreeGB:      PSObjectMapper.GetLong(dp, "FreeGB"),
                            FreePct:     PSObjectMapper.GetInt(dp, "FreePct"),
                            FileSystem:  PSObjectMapper.GetString(dp, "FileSystem")));

            // NICs
            var nics = new List<NICInfo>();
            if (obj.Properties["NICs"]?.Value is System.Collections.IEnumerable nicObjects)
                foreach (var n in nicObjects)
                    if (n is System.Management.Automation.PSObject np)
                        nics.Add(new NICInfo(
                            Description: PSObjectMapper.GetString(np, "Description"),
                            IPAddress:   PSObjectMapper.GetString(np, "IPAddress"),
                            MACAddress:  PSObjectMapper.GetString(np, "MACAddress")));

            return Result<HardwareInfo>.Success(new HardwareInfo(
                Manufacturer: PSObjectMapper.GetString(obj, "Manufacturer"),
                Model:        PSObjectMapper.GetString(obj, "Model"),
                SerialNumber: PSObjectMapper.GetString(obj, "SerialNumber"),
                BIOSVersion:  PSObjectMapper.GetString(obj, "BIOSVersion"),
                BIOSDate:     PSObjectMapper.GetString(obj, "BIOSDate"),
                CPUName:      PSObjectMapper.GetString(obj, "CPUName"),
                CPUCores:     PSObjectMapper.GetInt(obj, "CPUCores"),
                CPULogical:   PSObjectMapper.GetInt(obj, "CPULogical"),
                CPUSocket:    PSObjectMapper.GetString(obj, "CPUSocket"),
                CPUMaxMHz:    PSObjectMapper.GetInt(obj, "CPUMaxMHz"),
                TotalRAMGB:   PSObjectMapper.GetInt(obj, "TotalRAMGB"),
                RAMSlots:     ramSlots,
                GPUName:      PSObjectMapper.GetString(obj, "GPUName"),
                GPUVRAMMB:    PSObjectMapper.GetInt(obj, "GPUVRAMMB"),
                OSCaption:    PSObjectMapper.GetString(obj, "OSCaption"),
                OSBuild:      PSObjectMapper.GetString(obj, "OSBuild"),
                OSArch:       PSObjectMapper.GetString(obj, "OSArch"),
                OSInstall:    PSObjectMapper.GetString(obj, "OSInstall"),
                LastBoot:     PSObjectMapper.GetString(obj, "LastBoot"),
                Disks:        disks,
                NICs:         nics
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get hardware info");
            return Result<HardwareInfo>.Failure(ex.Message, ex);
        }
    }
}

// ─── Software ─────────────────────────────────────────────────────────────

public class SoftwareExecutor(RunspaceManager runspace, ILogger<SoftwareExecutor> logger)
    : IExecutorService<List<SoftwareItem>>
{
    public async Task<Result<List<SoftwareItem>>> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            var results = await runspace.InvokeAsync(
                EmbeddedScripts.Load("Get-InstalledSoftware.ps1"), ct);

            var items = results
                .Where(r => r is not null)
                .Select(r => new SoftwareItem(
                    Name:        PSObjectMapper.GetString(r, "Name"),
                    Version:     PSObjectMapper.GetString(r, "Version"),
                    Publisher:   PSObjectMapper.GetString(r, "Publisher"),
                    InstallDate: PSObjectMapper.GetDateTime(r, "InstallDate")
                ))
                .Where(i => !string.IsNullOrEmpty(i.Name))
                .OrderBy(i => i.Name)
                .ToList();

            return Result<List<SoftwareItem>>.Success(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get software inventory");
            return Result<List<SoftwareItem>>.Failure(ex.Message, ex);
        }
    }
}

// ─── Actions ──────────────────────────────────────────────────────────────

public class ActionExecutor(RunspaceManager runspace, ILogger<ActionExecutor> logger)
    : IActionExecutorService
{
    private static readonly IReadOnlyDictionary<CMActionType, string> _scheduleIds =
        new Dictionary<CMActionType, string>
        {
            [CMActionType.MachinePolicy]           = "{00000000-0000-0000-0000-000000000021}",
            [CMActionType.DiscoveryDataCollection] = "{00000000-0000-0000-0000-000000000003}",
            [CMActionType.SoftwareInventory]       = "{00000000-0000-0000-0000-000000000002}",
            [CMActionType.HardwareInventory]       = "{00000000-0000-0000-0000-000000000001}",
            [CMActionType.UpdateDeployment]        = "{00000000-0000-0000-0000-000000000108}",
            [CMActionType.UpdateScan]              = "{00000000-0000-0000-0000-000000000113}",
            [CMActionType.ApplicationDeployment]   = "{00000000-0000-0000-0000-000000000121}",
        };

    public async Task<Result> TriggerAsync(CMActionType action, CancellationToken ct = default)
    {
        if (!_scheduleIds.TryGetValue(action, out var scheduleId))
            return Result.Failure($"Unknown action: {action}");

        try
        {
            var script      = EmbeddedScripts.Load("Invoke-CMAction.ps1");
            var fullScript  = $"$ScheduleId = '{scheduleId}'\r\n{script}";
            var results     = await runspace.InvokeAsync(fullScript, ct);

            if (results.Count > 0)
            {
                var success = PSObjectMapper.GetBool(results[0], "Success");
                var message = PSObjectMapper.GetString(results[0], "Message");
                if (!success) return Result.Failure(message);
            }

            logger.LogInformation("Triggered CM action {Action}", action);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to trigger action {Action}", action);
            return Result.Failure($"{ex.GetType().Name}: {ex.Message}", ex);
        }
    }
}

// ─── Health Executor ──────────────────────────────────────────────────────

public class HealthExecutor(RunspaceManager runspace, ILogger<HealthExecutor> logger)
    : IHealthExecutorService
{
    public async Task<Result<List<HealthCheck>>> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            var results = await runspace.InvokeAsync(
                EmbeddedScripts.Load("Get-CMAgentHealth.ps1"), ct);

            var checks = results
                .Where(r => r is not null)
                .Select(r => new HealthCheck(
                    Category: PSObjectMapper.GetString(r, "Category"),
                    Name:     PSObjectMapper.GetString(r, "Name"),
                    Status:   PSObjectMapper.GetString(r, "Status"),
                    Value:    PSObjectMapper.GetString(r, "Value"),
                    Detail:   PSObjectMapper.GetString(r, "Detail")
                ))
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .ToList();

            return Result<List<HealthCheck>>.Success(checks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get health checks");
            return Result<List<HealthCheck>>.Failure(ex.Message, ex);
        }
    }
}

// ─── Embedded Script Loader ───────────────────────────────────────────────

internal static class EmbeddedScripts
{
    private static readonly System.Reflection.Assembly _asm = typeof(EmbeddedScripts).Assembly;

    public static string Load(string filename)
    {
        var resourceName = $"CMClientCenter.PowerShell.Scripts.{filename}";
        using var stream = _asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded script not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

// ─── Log Executor ─────────────────────────────────────────────────────────

public class LogExecutor(RunspaceManager runspace, ILogger<LogExecutor> logger)
{
    public async Task<Result<List<LogFileInfo>>> GetLogFilesAsync(CancellationToken ct = default)
    {
        try
        {
            var results = await runspace.InvokeAsync(
                EmbeddedScripts.Load("Get-CCMLogList.ps1"), ct);

            var files = results
                .Where(r => r is not null)
                .Select(r => new LogFileInfo(
                    Name:     PSObjectMapper.GetString(r, "Name"),
                    SizeKB:   PSObjectMapper.GetInt(r, "SizeMB"),
                    Modified: PSObjectMapper.GetString(r, "Modified"),
                    Folder:   PSObjectMapper.GetString(r, "Folder")
                ))
                .Where(f => !string.IsNullOrEmpty(f.Name))
                .ToList();

            return Result<List<LogFileInfo>>.Success(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get log files");
            return Result<List<LogFileInfo>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<LogEntry>>> GetLogEntriesAsync(
        string logName, int maxLines, CancellationToken ct = default)
    {
        try
        {
            var script = $"$LogName = '{logName}'\r\n$MaxLines = {maxLines}\r\n" +
                         EmbeddedScripts.Load("Get-CCMLogs.ps1");

            var results = await runspace.InvokeAsync(script, ct);

            // Fehler-Objekt abfangen
            if (results.Count == 1)
            {
                var err = PSObjectMapper.GetString(results[0], "Error");
                if (!string.IsNullOrEmpty(err))
                    return Result<List<LogEntry>>.Failure(err);
            }

            var entries = results
                .Where(r => r is not null)
                .Select(r => new LogEntry(
                    Time:      PSObjectMapper.GetString(r, "Time"),
                    Component: PSObjectMapper.GetString(r, "Component"),
                    Severity:  PSObjectMapper.GetString(r, "Severity"),
                    Message:   PSObjectMapper.GetString(r, "Message")
                ))
                .Where(e => !string.IsNullOrEmpty(e.Message))
                .ToList();

            return Result<List<LogEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get log entries for {LogName}", logName);
            return Result<List<LogEntry>>.Failure(ex.Message, ex);
        }
    }
}

// ─── LogService (implementiert ILogService aus Core) ─────────────────────

public class LogService(LogExecutor executor, ILogger<LogService> logger)
    : CMClientCenter.Core.Interfaces.ILogService
{
    public async Task<Result<List<LogFileInfo>>> GetLogFilesAsync(CancellationToken ct = default)
    {
        try { return await executor.GetLogFilesAsync(ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get log files");
            return Result<List<LogFileInfo>>.Failure(ex.Message, ex);
        }
    }

    public async Task<Result<List<LogEntry>>> GetLogEntriesAsync(
        string logName, int maxLines = 200, CancellationToken ct = default)
    {
        try { return await executor.GetLogEntriesAsync(logName, maxLines, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get log entries");
            return Result<List<LogEntry>>.Failure(ex.Message, ex);
        }
    }
}

// ─── Tools Executor ───────────────────────────────────────────────────────

public class ToolsExecutor(RunspaceManager runspace, ILogger<ToolsExecutor> logger)
    : CMClientCenter.Core.Interfaces.IToolsService
{
    public async Task<Result<CCMToolsInfo>> GetToolsInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await runspace.InvokeAsync(EmbeddedScripts.Load("Get-CCMTools.ps1"), ct);
            if (r.Count == 0) return Result<CCMToolsInfo>.Failure("No data");
            var o = r[0];

            // Query CacheItems separately — nested arrays are unreliable over WinRM
            var cacheItems = new List<CacheItem>();
            var cacheResults = await runspace.InvokeAsync(EmbeddedScripts.Load("Get-CCMCacheItems.ps1"), ct);
            foreach (var item in cacheResults)
            {
                var name = PSObjectMapper.GetString(item, "ContentId");
                if (string.IsNullOrEmpty(name)) continue;
                cacheItems.Add(new CacheItem(
                    name,
                    PSObjectMapper.GetString(item, "ContentVer"),
                    PSObjectMapper.GetString(item, "Location"),
                    PSObjectMapper.GetDouble(item, "SizeMB"),
                    PSObjectMapper.GetString(item, "LastRefTime")));
            }

            // RebootSources serialized as a pipe-delimited string — WinRM-safe
            var rebootSources = new List<string>();
            var raw = PSObjectMapper.GetString(o, "RebootSourcesRaw");
            if (!string.IsNullOrEmpty(raw))
                foreach (var s in raw.Split('|'))
                    if (!string.IsNullOrWhiteSpace(s)) rebootSources.Add(s.Trim());

            return Result<CCMToolsInfo>.Success(new CCMToolsInfo(
                PSObjectMapper.GetInt(o,"CacheSizeMB"), PSObjectMapper.GetInt(o,"CacheUsedMB"),
                PSObjectMapper.GetInt(o,"CacheFreeMB"), PSObjectMapper.GetString(o,"CachePath"),
                cacheItems, PSObjectMapper.GetBool(o,"RebootPending"), rebootSources,
                PSObjectMapper.GetBool(o,"CCMSetupRunning")));
        }
        catch (Exception ex) { logger.LogError(ex,"GetToolsInfo failed"); return Result<CCMToolsInfo>.Failure(ex.Message,ex); }
    }

    public async Task<Result> InvokeToolAsync(string action, CancellationToken ct = default)
    {
        try
        {
            var script = $"$ToolAction = '{action}'\r\n{EmbeddedScripts.Load("Invoke-CCMTool.ps1")}";
            var r = await runspace.InvokeAsync(script, ct);
            return ParseResult(r);
        }
        catch (Exception ex) { return Result.Failure(ex.Message,ex); }
    }

    private static Result ParseResult(List<System.Management.Automation.PSObject> r)
    {
        if (r.Count == 0) return Result.Success();
        var success = PSObjectMapper.GetBool(r[0], "Success");
        var message = PSObjectMapper.GetString(r[0], "Message");
        return success ? Result.Success() : Result.Failure(message);
    }
}

// Software Center: Applications (Install/Repair/Uninstall via CCM_Application).
// Split out of ToolsExecutor so "Tools" (cache/reboot/client repair) and
// "Software Center" (user-facing app catalog) stay independently testable
// and the Software Center page can later grow Task Sequences / OS deployment
// without dragging Tools-page concerns along.
public class SoftwareCenterExecutor(RunspaceManager runspace, ILogger<SoftwareCenterExecutor> logger)
    : CMClientCenter.Core.Interfaces.ISoftwareCenterService
{
    public async Task<Result<List<CCMApplication>>> GetApplicationsAsync(CancellationToken ct = default)
    {
        try
        {
            var apps = new List<CCMApplication>();
            var appResults = await runspace.InvokeAsync(EmbeddedScripts.Load("Get-CCMApplications.ps1"), ct);
            foreach (var app in appResults)
            {
                var name = PSObjectMapper.GetString(app, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                apps.Add(new CCMApplication(
                    PSObjectMapper.GetString(app,"Id"),      PSObjectMapper.GetString(app,"Revision"),
                    name,                                    PSObjectMapper.GetString(app,"Publisher"),
                    PSObjectMapper.GetString(app,"SoftwareVersion"),
                    PSObjectMapper.GetString(app,"InstallState"), PSObjectMapper.GetString(app,"ResolvedState")));
            }
            apps.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return Result<List<CCMApplication>>.Success(apps);
        }
        catch (Exception ex) { logger.LogError(ex,"GetApplications failed"); return Result<List<CCMApplication>>.Failure(ex.Message,ex); }
    }

    public async Task<Result> InvokeApplicationAsync(string appId, string revision, string action, CancellationToken ct = default)
    {
        try
        {
            var script = $"$AppId='{appId}'\r\n$AppRevision='{revision}'\r\n$AppAction='{action}'\r\n" +
                         EmbeddedScripts.Load("Invoke-CCMApplication.ps1");
            var r = await runspace.InvokeAsync(script, ct);
            return ParseResult(r);
        }
        catch (Exception ex) { return Result.Failure(ex.Message,ex); }
    }

    // Operating Systems: Task Sequences (inkl. OSD), gelesen aus CCM_Program
    // gefiltert auf TaskSequence=true (siehe Get-CCMTaskSequences.ps1).
    public async Task<Result<List<CCMTaskSequence>>> GetTaskSequencesAsync(CancellationToken ct = default)
    {
        try
        {
            var list = new List<CCMTaskSequence>();
            var tsResults = await runspace.InvokeAsync(EmbeddedScripts.Load("Get-CCMTaskSequences.ps1"), ct);
            foreach (var ts in tsResults)
            {
                var name = PSObjectMapper.GetString(ts, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                list.Add(new CCMTaskSequence(
                    PSObjectMapper.GetString(ts, "ProgramID"),
                    PSObjectMapper.GetString(ts, "PackageID"),
                    name,
                    PSObjectMapper.GetString(ts, "FullName"),
                    PSObjectMapper.GetString(ts, "PackageName"),
                    PSObjectMapper.GetString(ts, "Description"),
                    PSObjectMapper.GetString(ts, "Publisher"),
                    PSObjectMapper.GetString(ts, "Version"),
                    PSObjectMapper.GetBool(ts, "HighImpact"),
                    PSObjectMapper.GetBool(ts, "HighImpactTaskSequence"),
                    PSObjectMapper.GetBool(ts, "CustomHighImpactSet"),
                    PSObjectMapper.GetString(ts, "CustomHighImpactHeadline"),
                    PSObjectMapper.GetString(ts, "CustomHighImpactWarningTop"),
                    PSObjectMapper.GetString(ts, "CustomHighImpactWarning"),
                    PSObjectMapper.GetString(ts, "CustomHighImpactWarningInstall"),
                    PSObjectMapper.GetInt(ts, "EvaluationState"),
                    PSObjectMapper.GetString(ts, "LastRunStatus"),
                    PSObjectMapper.GetString(ts, "LastRunTime"),
                    PSObjectMapper.GetBool(ts, "RestartRequired"),
                    PSObjectMapper.GetBool(ts, "AdvertisedDirectly"),
                    PSObjectMapper.GetBool(ts, "Published")));
            }
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return Result<List<CCMTaskSequence>>.Success(list);
        }
        catch (Exception ex) { logger.LogError(ex, "GetTaskSequences failed"); return Result<List<CCMTaskSequence>>.Failure(ex.Message, ex); }
    }

    // ACHTUNG: Der Aufrufer (UI) MUSS vor diesem Call bereits den
    // High-Impact-Warn-Dialog bestaetigt haben (siehe CCMTaskSequence.HighImpact).
    // Dieses Script fuehrt nur aus, was die UI bereits freigegeben hat.
    public async Task<Result> InvokeTaskSequenceAsync(string programId, string packageId, CancellationToken ct = default)
    {
        try
        {
            var script = $"$TSProgramID='{programId}'\r\n$TSPackageID='{packageId}'\r\n" +
                         EmbeddedScripts.Load("Invoke-CCMTaskSequence.ps1");
            var r = await runspace.InvokeAsync(script, ct);
            return ParseResult(r);
        }
        catch (Exception ex) { return Result.Failure(ex.Message, ex); }
    }

    private static Result ParseResult(List<System.Management.Automation.PSObject> r)
    {
        if (r.Count == 0) return Result.Success();
        var success = PSObjectMapper.GetBool(r[0], "Success");
        var message = PSObjectMapper.GetString(r[0], "Message");
        return success ? Result.Success() : Result.Failure(message);
    }
}
