using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CMClientCenter.App.ViewModels;

public partial class DashboardViewModel(
    IConnectionService connectionService,
    ICMAgentService agentService,
    IHardwareService hardwareService) : ObservableObject
{
    [ObservableProperty] public partial CMAgentInfo? AgentInfo { get; set; }
    [ObservableProperty] public partial HardwareInfo? HardwareInfo { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!connectionService.IsConnected) return;
        AgentInfo = null; HardwareInfo = null;
        IsLoading = true; ErrorMessage = null;

        var agentResult = await agentService.GetAgentInfoAsync();
        if (agentResult.IsSuccess) AgentInfo = agentResult.Value;
        else ErrorMessage = agentResult.ErrorMessage;

        var hwResult = await hardwareService.GetHardwareInfoAsync();
        if (hwResult.IsSuccess) HardwareInfo = hwResult.Value;
        else ErrorMessage ??= hwResult.ErrorMessage;

        IsLoading = false;
    }
}

public partial class AgentStatusViewModel(ICMAgentService agentService) : ObservableObject
{
    [ObservableProperty] public partial CMAgentInfo? AgentInfo { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true; ErrorMessage = null;
        var result = await agentService.GetAgentInfoAsync();
        if (result.IsSuccess) AgentInfo = result.Value;
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }
}

public partial class HardwareViewModel(IHardwareService hardwareService) : ObservableObject
{
    [ObservableProperty] public partial HardwareInfo? HardwareInfo { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        var result = await hardwareService.GetHardwareInfoAsync();
        if (result.IsSuccess) HardwareInfo = result.Value;
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }
}

public partial class SoftwareViewModel(ISoftwareService softwareService) : ObservableObject
{
    [ObservableProperty] public partial List<SoftwareItem> Items { get; set; } = [];
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string Filter { get; set; } = string.Empty;

    public IEnumerable<SoftwareItem> FilteredItems =>
        string.IsNullOrWhiteSpace(Filter) ? Items
        : Items.Where(i =>
            i.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
            i.Publisher.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
            i.Version.Contains(Filter, StringComparison.OrdinalIgnoreCase));

    partial void OnFilterChanged(string value) => OnPropertyChanged(nameof(FilteredItems));
    partial void OnItemsChanged(List<SoftwareItem> value) => OnPropertyChanged(nameof(FilteredItems));

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        var result = await softwareService.GetInstalledSoftwareAsync();
        if (result.IsSuccess) Items = result.Value ?? [];
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }
}

public partial class ActionsViewModel(IActionService actionService) : ObservableObject
{
    public IReadOnlyList<CMAction> Actions => actionService.GetAvailableActions();
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string? LastResult { get; set; }

    [RelayCommand]
    private async Task TriggerActionAsync(CMAction action)
    {
        IsBusy = true; LastResult = null;
        var result = await actionService.TriggerActionAsync(action.ActionType);
        LastResult = result.IsSuccess
            ? $"✓ {action.Name} triggered successfully"
            : $"✗ Error: {result.ErrorMessage}";
        IsBusy = false;
    }
}

public partial class AgentHealthViewModel(IAgentHealthService healthService) : ObservableObject
{
    [ObservableProperty] public partial List<HealthCheck> Checks { get; set; } = [];
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public int HealthyCount => Checks.Count(c => c.Status == "Healthy");
    public int WarningCount => Checks.Count(c => c.Status == "Warning");
    public int ErrorCount   => Checks.Count(c => c.Status == "Error");
    public IEnumerable<IGrouping<string, HealthCheck>> GroupedChecks => Checks.GroupBy(c => c.Category);

    partial void OnChecksChanged(List<HealthCheck> value)
    {
        OnPropertyChanged(nameof(HealthyCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(GroupedChecks));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true; ErrorMessage = null; Checks = [];
        var result = await healthService.GetHealthChecksAsync();
        if (result.IsSuccess) Checks = result.Value ?? [];
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }
}

public partial class LogsViewModel(ILogService logService) : ObservableObject
{
    [ObservableProperty] public partial List<LogFileInfo> LogFiles { get; set; } = [];
    [ObservableProperty] public partial LogFileInfo? SelectedLog { get; set; }
    [ObservableProperty] public partial List<LogEntry> Entries { get; set; } = [];
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string Filter { get; set; } = string.Empty;
    [ObservableProperty] public partial int MaxLines { get; set; } = 200;

    public IEnumerable<LogEntry> FilteredEntries =>
        string.IsNullOrWhiteSpace(Filter) ? Entries
        : Entries.Where(e =>
            e.Message.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
            e.Component.Contains(Filter, StringComparison.OrdinalIgnoreCase));

    partial void OnFilterChanged(string value) => OnPropertyChanged(nameof(FilteredEntries));
    partial void OnEntriesChanged(List<LogEntry> value) => OnPropertyChanged(nameof(FilteredEntries));

    [RelayCommand]
    private async Task LoadLogFilesAsync()
    {
        IsLoading = true; ErrorMessage = null;
        var result = await logService.GetLogFilesAsync();
        if (result.IsSuccess) LogFiles = result.Value ?? [];
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task LoadEntriesAsync()
    {
        if (SelectedLog is null) return;
        IsLoading = true; ErrorMessage = null; Entries = [];
        var result = await logService.GetLogEntriesAsync(SelectedLog.Name, MaxLines);
        if (result.IsSuccess) Entries = result.Value ?? [];
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }
}

public partial class ToolsViewModel(IToolsService toolsService) : ObservableObject
{
    [ObservableProperty] private CCMToolsInfo? _toolsInfo;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _lastResult;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true; ErrorMessage = null;
        var result = await toolsService.GetToolsInfoAsync();
        if (result.IsSuccess) ToolsInfo = result.Value;
        else ErrorMessage = result.ErrorMessage;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task InvokeToolAsync(string action)
    {
        IsBusy = true; LastResult = null;
        var result = await toolsService.InvokeToolAsync(action);
        LastResult = result.IsSuccess ? $"✓ Success" : $"✗ {result.ErrorMessage}";
        IsBusy = false;
        if (result.IsSuccess) await RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task InvokeApplicationAsync((string id, string rev, string action) p)
    {
        IsBusy = true; LastResult = null;
        var result = await toolsService.InvokeApplicationAsync(p.id, p.rev, p.action);
        LastResult = result.IsSuccess ? $"✓ {p.action} started" : $"✗ {result.ErrorMessage}";
        IsBusy = false;
    }
}
