using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace CMClientCenter.App.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; } =
        App.Services.GetRequiredService<LogsViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private static readonly int[] _maxLinesOptions = [100, 200, 500, 1000];

    // Set by EntriesList_RightTapped just before the context menu opens, so
    // CopyLineMenuItem_Click knows which row it applies to. ListView items
    // are virtualized/reused, so this can't be resolved any other way once
    // the click actually fires.
    private LogEntry? _rightTappedEntry;

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
        // Cast to PivotItem (rather than comparing as object) — same reference
        // comparison either way since PivotItem doesn't override ==/Equals, but
        // explicit about intent and avoids CS0252's "did you mean a value
        // comparison?" warning.
        var selected = SourcePivot.SelectedItem as PivotItem;
        CcmLogFileList.Visibility      = selected == CcmTab      ? Visibility.Visible : Visibility.Collapsed;
        CcmSetupLogFileList.Visibility = selected == CcmSetupTab ? Visibility.Visible : Visibility.Collapsed;
        PsadtLogFileList.Visibility    = selected == PsadtTab    ? Visibility.Visible : Visibility.Collapsed;

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

    // Copies every currently-filtered log entry to the clipboard as plain
    // tab-separated text (Time, Component, Message — one line each), so the
    // whole visible log can be pasted into Notepad/Excel/an email at once.
    // This is the ListView equivalent of "select all + copy" on the
    // Console page's output TextBox.
    private void CopyAllButton_Click(object sender, RoutedEventArgs e)
    {
        var entries = ViewModel.FilteredEntries.ToList();
        if (entries.Count == 0) return;

        var sb = new StringBuilder();
        foreach (var entry in entries)
            sb.AppendLine($"{entry.Time}\t{entry.Component}\t{entry.Message}");

        CopyToClipboard(sb.ToString());
    }

    // Right-click on a row: resolve which LogEntry is under the pointer
    // (walking up from the tapped element to its containing ListViewItem,
    // since ItemTemplate content doesn't carry the entry directly) and show
    // the "Copy line" flyout at the pointer position.
    private void EntriesList_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement element) return;

        var container = FindContainer(element);
        if (container?.DataContext is not LogEntry entry) return;

        _rightTappedEntry = entry;

        var flyout = (MenuFlyout)Resources["EntryContextMenu"];
        flyout.ShowAt(EntriesList, e.GetPosition(EntriesList));
        e.Handled = true;
    }

    // Walks up the visual tree from the tapped element until it finds the
    // ListViewItem container, whose DataContext is the bound LogEntry.
    private static FrameworkElement? FindContainer(FrameworkElement element)
    {
        FrameworkElement? current = element;
        while (current is not null && current is not ListViewItem)
            current = VisualTreeHelper.GetParent(current) as FrameworkElement;
        return current;
    }

    private void CopyLineMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_rightTappedEntry is not { } entry) return;

        CopyToClipboard($"{entry.Time}\t{entry.Component}\t{entry.Message}");
        _rightTappedEntry = null;
    }

    // Wrapped in try/catch: Clipboard.SetContent is a WinRT API that can
    // throw CO_E_NOTINITIALIZED in some unpackaged-app edge cases (e.g. if a
    // future change ever calls this off the UI thread). Main() is already
    // [STAThread] and synchronous, which is the documented fix, but this
    // keeps a copy failure from ever crashing the app — it just silently
    // no-ops, same risk tier as a clipboard write failing on a locked
    // clipboard in any other Windows app.
    private void CopyToClipboard(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
        catch (Exception)
        {
            // Best-effort; nothing the user can act on here.
        }
    }
}
