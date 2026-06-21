using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App.Views;

public sealed partial class UpdatesPage : Page
{
    public UpdatesViewModel ViewModel { get; } =
        App.Services.GetRequiredService<UpdatesViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private List<CCMSoftwareUpdate> _allUpdates = [];

    public UpdatesPage()
    {
        InitializeComponent();

        RefreshButton.Click += async (_, _) =>
            await ViewModel.RefreshCommand.ExecuteAsync(null);

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(UpdatesViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading
                            ? Microsoft.UI.Xaml.Visibility.Visible
                            : Microsoft.UI.Xaml.Visibility.Collapsed;
                        RefreshButton.IsEnabled = !ViewModel.IsLoading;
                        break;

                    case nameof(UpdatesViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;

                    case nameof(UpdatesViewModel.LastResult):
                        ShowResult(ViewModel.LastResult);
                        break;

                    case nameof(UpdatesViewModel.Updates):
                        _allUpdates = ViewModel.Updates;
                        ApplyAllFilter(AllFilterBox.Text);
                        ApplyPendingFilter(PendingFilterBox.Text);
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionStateChanged;

        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected && ViewModel.Updates.Count == 0)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        };

        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionStateChanged;
    }

    private async void OnConnectionStateChanged(object? sender, ConnectionResult connResult)
    {
        if (connResult.IsConnected)
        {
            await Task.Delay(500); // Warten bis Runspace bereit
            await ViewModel.RefreshCommand.ExecuteAsync(null);
        }
        else
        {
            _dispatcher.TryEnqueue(() =>
            {
                _allUpdates = [];
                AllList.ItemsSource = null;
                PendingList.ItemsSource = null;
                AllCountText.Text = "0";
                PendingCountText.Text = "0";
                AllFilterBox.Text = "";
                PendingFilterBox.Text = "";
                ErrorBar.IsOpen = false;
                ResultBar.IsOpen = false;
            });
        }
    }

    private void AllFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyAllFilter(AllFilterBox.Text);

    private void ApplyAllFilter(string filter)
    {
        var filtered = ApplyTextFilter(_allUpdates, filter);
        AllList.ItemsSource = filtered;
        AllCountText.Text = string.IsNullOrWhiteSpace(filter)
            ? $"{filtered.Count}"
            : $"{filtered.Count} / {_allUpdates.Count}";
    }

    private void PendingFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyPendingFilter(PendingFilterBox.Text);

    private void ApplyPendingFilter(string filter)
    {
        // "Pending" = Status=="Missing" aus CCM_UpdateStatus — siehe
        // Get-CCMSoftwareUpdates.ps1. Kein zweiter Service-Call notwendig,
        // beide Tabs filtern dieselbe geladene Liste.
        var pendingOnly = _allUpdates.Where(u => u.Status.Equals("Missing", StringComparison.OrdinalIgnoreCase)).ToList();
        var filtered = ApplyTextFilter(pendingOnly, filter);
        PendingList.ItemsSource = filtered;
        PendingCountText.Text = string.IsNullOrWhiteSpace(filter)
            ? $"{filtered.Count}"
            : $"{filtered.Count} / {pendingOnly.Count}";
    }

    private static List<CCMSoftwareUpdate> ApplyTextFilter(List<CCMSoftwareUpdate> source, string filter)
        => string.IsNullOrWhiteSpace(filter)
            ? source
            : source.Where(u =>
                u.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                u.Article.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                u.Bulletin.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();

    private async void InstallUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not CCMSoftwareUpdate update) return;
        if (string.IsNullOrEmpty(update.InstallableUpdateId)) return; // sollte durch IsEnabled-Binding bereits verhindert sein

        ResultBar.IsOpen = false;
        await ViewModel.InstallUpdateCommand.ExecuteAsync(update.InstallableUpdateId!);
    }

    private void ShowResult(string? result)
    {
        if (result is null) return;
        var success = result.StartsWith("✓");
        ResultBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        ResultBar.Message  = result;
        ResultBar.IsOpen   = true;
    }
}
