using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App.Views;

public sealed partial class SoftwareCenterPage : Page
{
    public SoftwareCenterViewModel ViewModel { get; } =
        App.Services.GetRequiredService<SoftwareCenterViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private List<CCMApplication> _allApps = [];
    private List<CCMTaskSequence> _allTaskSequences = [];

    public SoftwareCenterPage()
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
                    case nameof(SoftwareCenterViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading
                            ? Microsoft.UI.Xaml.Visibility.Visible
                            : Microsoft.UI.Xaml.Visibility.Collapsed;
                        RefreshButton.IsEnabled = !ViewModel.IsLoading;
                        break;

                    case nameof(SoftwareCenterViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;

                    case nameof(SoftwareCenterViewModel.LastResult):
                        ShowResult(ViewModel.LastResult);
                        break;

                    case nameof(SoftwareCenterViewModel.Applications):
                        _allApps = ViewModel.Applications;
                        ApplyAppsFilter(AppsFilterBox.Text);
                        break;

                    case nameof(SoftwareCenterViewModel.TaskSequences):
                        _allTaskSequences = ViewModel.TaskSequences;
                        ApplyTSFilter(TSFilterBox.Text);
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionStateChanged;

        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected && ViewModel.Applications.Count == 0 && ViewModel.TaskSequences.Count == 0)
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
                _allApps = [];
                _allTaskSequences = [];
                AppsList.ItemsSource = null;
                TSList.ItemsSource = null;
                AppsCountText.Text = "0";
                TSCountText.Text = "0";
                AppsFilterBox.Text = "";
                TSFilterBox.Text = "";
                ErrorBar.IsOpen = false;
                ResultBar.IsOpen = false;
            });
        }
    }

    private void AppsFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyAppsFilter(AppsFilterBox.Text);

    private void ApplyAppsFilter(string filter)
    {
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allApps
            : _allApps.Where(a =>
                a.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                a.Publisher.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                a.SoftwareVersion.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        AppsList.ItemsSource = filtered;
        AppsCountText.Text = string.IsNullOrWhiteSpace(filter)
            ? $"{filtered.Count}"
            : $"{filtered.Count} / {_allApps.Count}";
    }

    private void TSFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyTSFilter(TSFilterBox.Text);

    private void ApplyTSFilter(string filter)
    {
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allTaskSequences
            : _allTaskSequences.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                t.Publisher.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                t.Version.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        TSList.ItemsSource = filtered;
        TSCountText.Text = string.IsNullOrWhiteSpace(filter)
            ? $"{filtered.Count}"
            : $"{filtered.Count} / {_allTaskSequences.Count}";
    }

    private async void RunTaskSequence_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not CCMTaskSequence ts) return;

        if (ts.HighImpact)
        {
            var confirmed = await ShowHighImpactConfirmationAsync(ts);
            if (!confirmed) return;
        }

        ResultBar.IsOpen = false;
        await ViewModel.InvokeTaskSequenceCommand.ExecuteAsync((ts.ProgramId, ts.PackageId));
    }

    // High-Impact-Bestaetigung: verwendet die in ConfigMgr vom Admin gepflegten
    // CustomHighImpact*-Texte (mehrsprachig vorhanden), statt einen eigenen
    // Warntext zu erfinden. Fallback auf generischen Text, falls nicht gesetzt.
    private async Task<bool> ShowHighImpactConfirmationAsync(CCMTaskSequence ts)
    {
        var headline = ts.CustomHighImpactSet && !string.IsNullOrWhiteSpace(ts.CustomHighImpactHeadline)
            ? ts.CustomHighImpactHeadline
            : "This task sequence has high impact and may reinstall the operating system, " +
              "which can result in data loss on the target computer.";

        var detail = ts.CustomHighImpactSet && !string.IsNullOrWhiteSpace(ts.CustomHighImpactWarningTop)
            ? ts.CustomHighImpactWarningTop
            : null;

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = headline, TextWrapping = TextWrapping.Wrap });
        if (detail is not null)
            panel.Children.Add(new TextBlock
            {
                Text = detail,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.8
            });

        var confirmCheckbox = new CheckBox { Content = "I understand the consequences of running this task sequence." };
        panel.Children.Add(confirmCheckbox);

        var dialog = new ContentDialog
        {
            Title = $"Run \"{ts.Name}\"?",
            Content = panel,
            PrimaryButtonText = "Run",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            IsPrimaryButtonEnabled = false,
            XamlRoot = this.XamlRoot
        };

        confirmCheckbox.Checked   += (_, _) => dialog.IsPrimaryButtonEnabled = true;
        confirmCheckbox.Unchecked += (_, _) => dialog.IsPrimaryButtonEnabled = false;

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async void AppAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item) return;
        if (item.Tag is not CCMApplication app) return;
        var action = item.Text; // "Install" | "Repair" | "Uninstall"

        ResultBar.IsOpen = false;
        await ViewModel.InvokeApplicationCommand.ExecuteAsync((app.Id, app.Revision, action));
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
