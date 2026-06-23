using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CMClientCenter.App.Views;

public sealed partial class ConsolePage : Page
{
    public ConsoleViewModel ViewModel { get; } =
        App.Services.GetRequiredService<ConsoleViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly IAppSettingsService _settingsService =
        App.Services.GetRequiredService<IAppSettingsService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public ConsolePage()
    {
        InitializeComponent();

        ScriptsFolderText.Text = _settingsService.EffectiveScriptsFolder;

        // Restore the output panel width from a previous session, if the user
        // ever dragged the splitter — otherwise the 420px default already set
        // in ConsolePage.xaml's OutputColumn definition stays untouched.
        if (_settingsService.Current.ConsoleOutputColumnWidth is { } savedWidth)
        {
            var clamped = Math.Clamp(savedWidth, MinOutputColumnWidth, MaxOutputColumnWidth);
            OutputColumn.Width        = new GridLength(clamped);
            _currentOutputColumnWidth = clamped;
        }

        RefreshButton.Click          += async (_, _) => await ViewModel.RefreshScriptsCommand.ExecuteAsync(null);
        OpenConsoleButton.Click      += (_, _) => ViewModel.OpenConsoleCommand.Execute(null);
        ClearOutputButton.Click      += (_, _) => OutputText.Text = "";
        OpenScriptsFolderButton.Click += (_, _) => OpenScriptsFolder();

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ConsoleViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case nameof(ConsoleViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;
                    case nameof(ConsoleViewModel.BuiltinScriptGroups):
                    case nameof(ConsoleViewModel.CustomScriptGroups):
                        UpdateScriptsLists();
                        break;
                    case nameof(ConsoleViewModel.IsBusy):
                        OutputBusyRing.IsActive = ViewModel.IsBusy;
                        break;
                    case nameof(ConsoleViewModel.LastResult):
                        LastResultText.Text = ViewModel.LastResult ?? "";
                        break;
                    case nameof(ConsoleViewModel.ScriptOutput):
                        // Output is now in its own always-visible panel (no more
                        // scrolling past the script lists to see it), but a long
                        // script's output can still scroll within that panel —
                        // reset to the top so the start of the new run is visible
                        // instead of wherever the previous run's scroll position was.
                        OutputText.Text = ViewModel.ScriptOutput ?? "";
                        OutputScrollViewer.ChangeView(null, 0, null, true);
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionChanged;
        UpdateConnectionUI(_connectionService.IsConnected);

        Loaded += async (_, _) => await ViewModel.RefreshScriptsCommand.ExecuteAsync(null);
        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionChanged;
    }

    private void OnConnectionChanged(object? sender, ConnectionResult r) =>
        _dispatcher.TryEnqueue(() => UpdateConnectionUI(r.IsConnected));

    private void UpdateConnectionUI(bool connected)
    {
        NotConnectedBar.IsOpen      = !connected;
        OpenConsoleButton.IsEnabled = connected;
    }

    private void UpdateScriptsLists()
    {
        // CollectionViewSource.Source expects a collection of groups, where
        // each group is itself enumerable (ScriptGroup : List<CustomScriptInfo>
        // already satisfies that) — standard WinUI 3 grouped-ListView pattern.
        BuiltinScriptsGroupedSource.Source = ViewModel.BuiltinScriptGroups;
        BuiltinScriptsList.ItemsSource      = BuiltinScriptsGroupedSource.View;
        NoBuiltinScriptsText.Visibility     = ViewModel.BuiltinScriptGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        CustomScriptsGroupedSource.Source = ViewModel.CustomScriptGroups;
        CustomScriptsList.ItemsSource      = CustomScriptsGroupedSource.View;
        NoCustomScriptsText.Visibility     = ViewModel.CustomScriptGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void RunScript_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CustomScriptInfo script }) return;

        // Built-in scripts can do impactful things (service restarts, WMI repair,
        // etc. — see PSScripts/LICENSE-and-SOURCE.md) and the person running them
        // didn't necessarily write them, unlike their own custom scripts — so they
        // get the same "are you sure" treatment as OSD deployments on the Software
        // Center page (ContentDialog + mandatory confirmation checkbox).
        if (script.IsBuiltin && !await ConfirmBuiltinScriptAsync(script))
            return;

        await ViewModel.RunScriptCommand.ExecuteAsync(script.FullPath);
    }

    private async Task<bool> ConfirmBuiltinScriptAsync(CustomScriptInfo script)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "This is a built-in script that may change settings or restart services " +
                   "on the connected computer. Review the script file first if you're unsure what it does.",
            TextWrapping = TextWrapping.Wrap
        });

        var confirmCheckbox = new CheckBox { Content = "I understand what this script does and want to run it." };
        panel.Children.Add(confirmCheckbox);

        var dialog = new ContentDialog
        {
            Title                  = $"Run \"{script.Name}\"?",
            Content                = panel,
            PrimaryButtonText      = "Run",
            CloseButtonText        = "Cancel",
            DefaultButton          = ContentDialogButton.Close,
            IsPrimaryButtonEnabled = false,
            XamlRoot               = this.XamlRoot
        };

        confirmCheckbox.Checked   += (_, _) => dialog.IsPrimaryButtonEnabled = true;
        confirmCheckbox.Unchecked += (_, _) => dialog.IsPrimaryButtonEnabled = false;

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private void OpenScriptsFolder()
    {
        try
        {
            var folder = _settingsService.EffectiveScriptsFolder;
            System.IO.Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = folder,
                UseShellExecute = true
            });
        }
        catch
        {
            // Non-critical — the path is shown in the UI either way.
        }
    }

    // --- Output panel resize splitter ---------------------------------------
    // Plain pointer-event dragging instead of the CommunityToolkit GridSplitter
    // (see the XAML comment next to OutputSplitter for why). Resizes
    // OutputColumn directly; the left ScrollViewer's Grid.Column="0" is Width="*"
    // so it automatically takes whatever space is left.

    private bool _isDraggingSplitter;
    private double _dragStartPointerX;
    private double _dragStartColumnWidth;
    private double _currentOutputColumnWidth = 420; // kept in sync with OutputColumn.Width while dragging

    private const double MinOutputColumnWidth = 260;
    private const double MaxOutputColumnWidth = 900;

    private void OutputSplitter_PointerEntered(object sender, PointerRoutedEventArgs e) =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

    private void OutputSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingSplitter)
            ProtectedCursor = null;
    }

    private void OutputSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSplitter   = true;
        _dragStartPointerX    = e.GetCurrentPoint(this).Position.X;
        _dragStartColumnWidth = OutputColumn.ActualWidth;
        OutputSplitter.CapturePointer(e.Pointer);
    }

    private void OutputSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingSplitter) return;

        // Dragging left grows the output column (mouse moves toward the script
        // lists), dragging right shrinks it — hence the sign flip.
        var deltaX   = e.GetCurrentPoint(this).Position.X - _dragStartPointerX;
        var newWidth = _dragStartColumnWidth - deltaX;
        newWidth     = Math.Clamp(newWidth, MinOutputColumnWidth, MaxOutputColumnWidth);

        OutputColumn.Width        = new GridLength(newWidth);
        _currentOutputColumnWidth = newWidth;
    }

    private async void OutputSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSplitter = false;
        OutputSplitter.ReleasePointerCapture(e.Pointer);
        ProtectedCursor = null;

        // Skip the write if nothing actually changed (e.g. a plain click on
        // the splitter with no drag) — no point touching the settings file
        // for a no-op.
        if (_settingsService.Current.ConsoleOutputColumnWidth == _currentOutputColumnWidth)
            return;

        // Persist only on release, not on every PointerMoved — dragging fires
        // far too many move events to write the settings file on each one.
        // Use the value we tracked during PointerMoved rather than
        // OutputColumn.ActualWidth, since the layout pass that updates
        // ActualWidth isn't guaranteed to have run yet at this exact point.
        var updated = _settingsService.Current with { ConsoleOutputColumnWidth = _currentOutputColumnWidth };
        try
        {
            await _settingsService.SaveAsync(updated);
        }
        catch
        {
            // Non-critical — worst case the chosen width just isn't remembered
            // next time; not worth bothering the user with an error for this.
        }
    }
}
