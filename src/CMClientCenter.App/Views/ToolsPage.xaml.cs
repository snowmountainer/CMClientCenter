using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CMClientCenter.App.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsViewModel ViewModel { get; } =
        App.Services.GetRequiredService<ToolsViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public ToolsPage()
    {
        InitializeComponent();

        RefreshButton.Click      += async (_, _) => await ViewModel.RefreshCommand.ExecuteAsync(null);
        ClearCacheButton.Click   += async (_, _) => await RunTool("ClearCache");
        RepairButton.Click       += async (_, _) => await RunTool("RepairClient");
        ReinstallButton.Click    += async (_, _) => await RunTool("ReinstallClient");
        RebootButton.Click       += async (_, _) => await RunTool("RebootNow");
        CancelRebootButton.Click += async (_, _) => await RunTool("CancelReboot");

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ToolsViewModel.IsLoading):
                        LoadingBar.Visibility      = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case nameof(ToolsViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;
                    case nameof(ToolsViewModel.LastResult):
                        ShowResult(ViewModel.LastResult);
                        break;
                    case nameof(ToolsViewModel.ToolsInfo):
                        UpdateUI();
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionChanged;
        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        };
        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionChanged;
    }

    private async void OnConnectionChanged(object? sender, ConnectionResult r)
    {
        if (r.IsConnected) { await Task.Delay(400); await ViewModel.RefreshCommand.ExecuteAsync(null); }
        else _dispatcher.TryEnqueue(ResetUI);
    }

    private async Task RunTool(string action)
    {
        try
        {
            SetButtonsEnabled(false);
            ResultBar.IsOpen = false;
            await ViewModel.InvokeToolCommand.ExecuteAsync(action);
        }
        catch (Exception ex)
        {
            _dispatcher.TryEnqueue(() =>
            {
                ResultBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                ResultBar.Message  = $"Error: {ex.Message}";
                ResultBar.IsOpen   = true;
            });
        }
        finally
        {
            _dispatcher.TryEnqueue(() => SetButtonsEnabled(_connectionService.IsConnected));
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        ClearCacheButton.IsEnabled   = enabled;
        RepairButton.IsEnabled       = enabled;
        ReinstallButton.IsEnabled    = enabled;
        RebootButton.IsEnabled       = enabled;
        CancelRebootButton.IsEnabled = enabled;
    }

    private void ShowResult(string? result)
    {
        if (result is null) return;
        var success      = result.StartsWith("✓");
        ResultBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        ResultBar.Message  = result;
        ResultBar.IsOpen   = true;
    }

    private void UpdateUI()
    {
        var info = ViewModel.ToolsInfo;
        if (info is null) { ResetUI(); return; }

        CachePath.Text        = info.CachePath;
        CacheSize.Text        = $"{info.CacheSizeMB} MB";
        CacheUsed.Text        = $"{info.CacheUsedMB} MB";
        CacheFree.Text        = $"{info.CacheFreeMB} MB";
        CacheItemsHeader.Text = $"Cache Items ({info.CacheItems.Count})";
        CacheItemsList.ItemsSource = info.CacheItems;

        CCMSetupBar.IsOpen = info.CCMSetupRunning;

        RebootStatus.Text       = info.RebootPending ? "⚠ Restart pending" : "✓ No restart required";
        RebootStatus.Foreground = new SolidColorBrush(info.RebootPending ? Colors.DarkOrange : Colors.ForestGreen);
        RebootSources.Text      = info.RebootSources.Count > 0
            ? "Sources: " + string.Join(", ", info.RebootSources) : "";
    }

    private void ResetUI()
    {
        CachePath.Text = CacheSize.Text = CacheUsed.Text = CacheFree.Text = "-";
        CacheItemsHeader.Text = "Cache Items (0)";
        CacheItemsList.ItemsSource = null;
        RebootStatus.Text = "-";
        RebootSources.Text = "";
        CCMSetupBar.IsOpen = false;
        ResultBar.IsOpen = ErrorBar.IsOpen = false;
    }
}
