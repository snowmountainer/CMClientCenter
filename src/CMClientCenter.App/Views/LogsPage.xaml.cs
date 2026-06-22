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

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; } =
        App.Services.GetRequiredService<LogsViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private static readonly int[] _maxLinesOptions = [100, 200, 500, 1000];

    public LogsPage()
    {
        InitializeComponent();

        RefreshFilesButton.Click += async (_, _) =>
            await ViewModel.LoadLogFilesCommand.ExecuteAsync(null);

        RefreshLogButton.Click += async (_, _) =>
            await ViewModel.LoadEntriesCommand.ExecuteAsync(null);

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(LogsViewModel.IsLoading):
                        LoadingBar.Visibility      = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        RefreshLogButton.IsEnabled = !ViewModel.IsLoading;
                        break;
                    case nameof(LogsViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;
                    case nameof(LogsViewModel.LogFiles):
                        // Each tab gets its own pre-filtered list from the ViewModel
                        CcmLogFileList.ItemsSource      = ViewModel.CcmLogFiles;
                        CcmSetupLogFileList.ItemsSource = ViewModel.CcmSetupLogFiles;
                        PsadtLogFileList.ItemsSource    = ViewModel.PsadtLogFiles;
                        CcmCountText.Text               = ViewModel.CcmLogCount.ToString();
                        CcmSetupCountText.Text          = ViewModel.CcmSetupLogCount.ToString();
                        PsadtCountText.Text              = ViewModel.PsadtLogCount.ToString();
                        break;
                    case nameof(LogsViewModel.Entries):
                    case nameof(LogsViewModel.FilteredEntries):
                        EntriesList.ItemsSource = ViewModel.FilteredEntries.ToList();
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionChanged;

        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected)
                await ViewModel.LoadLogFilesCommand.ExecuteAsync(null);
        };

        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionChanged;
    }

    private async void OnConnectionChanged(object? sender, ConnectionResult r)
    {
        if (r.IsConnected)
        {
            await Task.Delay(400);
            await ViewModel.LoadLogFilesCommand.ExecuteAsync(null);
        }
        else
        {
            _dispatcher.TryEnqueue(() =>
            {
                CcmLogFileList.ItemsSource      = null;
                CcmSetupLogFileList.ItemsSource = null;
                PsadtLogFileList.ItemsSource    = null;
                EntriesList.ItemsSource         = null;
                LogTitle.Text                   = "Logs";
                ErrorBar.IsOpen                 = false;
            });
        }
    }

    // Switching tabs shows the matching file list and clears the viewer so
    // the user isn't left looking at a file from another source.
    private void SourcePivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CcmLogFileList.Visibility      = SourcePivot.SelectedItem == CcmTab      ? Visibility.Visible : Visibility.Collapsed;
        CcmSetupLogFileList.Visibility = SourcePivot.SelectedItem == CcmSetupTab ? Visibility.Visible : Visibility.Collapsed;
        PsadtLogFileList.Visibility    = SourcePivot.SelectedItem == PsadtTab    ? Visibility.Visible : Visibility.Collapsed;

        CcmLogFileList.SelectedItem      = null;
        CcmSetupLogFileList.SelectedItem = null;
        PsadtLogFileList.SelectedItem    = null;

        ViewModel.SelectedLog = null;
        ViewModel.Entries     = [];
        LogTitle.Text         = "Logs";
        FilterBox.Text        = "";
    }

    // Shared handler for all three per-tab ListViews
    private async void LogFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListView list || list.SelectedItem is not LogFileInfo logFile) return;

        ViewModel.SelectedLog = logFile;
        LogTitle.Text         = logFile.Name;
        FilterBox.Text        = "";
        await ViewModel.LoadEntriesCommand.ExecuteAsync(null);
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.Filter = FilterBox.Text;
    }

    private void MaxLinesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MaxLinesBox.SelectedIndex >= 0 && MaxLinesBox.SelectedIndex < _maxLinesOptions.Length)
            ViewModel.MaxLines = _maxLinesOptions[MaxLinesBox.SelectedIndex];
    }
}
