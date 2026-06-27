using CMClientCenter.App.Views;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.UI;

namespace CMClientCenter.App;

public sealed partial class MainWindow : Window
{
    private readonly IAppSettingsService _settingsService;

    // Tracks the window's last known RESTORED (non-maximized, non-minimized)
    // position/size. AppWindow.Position/Size report the maximized bounds
    // while maximized, so we can't just read them at Closing time — we
    // need to remember what they were right before the window maximized.
    private RectInt32? _lastRestoredBounds;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        _settingsService = App.Services.GetRequiredService<IAppSettingsService>();

        RestoreWindowGeometry();

        // Apply the persisted theme BEFORE navigating to the first page.
        // Pages resolve ThemeResource bindings at load time — navigating
        // first and setting the theme afterwards leaves already-loaded
        // pages stuck on the wrong theme even though RequestedTheme changes.
        // Note: pass 'this' explicitly — App.MainAppWindow is still null
        // at this point because 'new MainWindow()' hasn't returned yet.
        App.ApplyTheme(this, _settingsService.Current.Theme);
        _settingsService.SettingsChanged += (_, settings) =>
        {
            // Defer to the next dispatcher cycle: SettingsChanged can fire
            // synchronously from inside a control's own event handler (e.g.
            // ComboBox.SelectionChanged on the SettingsPage). Modifying the
            // visual tree's RequestedTheme while that control is still mid-way
            // through processing its own event causes a native crash.
            DispatcherQueue.TryEnqueue(() => App.ApplyTheme(this, settings.Theme));
        };

        // Caption buttons (minimize/maximize/close) don't follow RequestedTheme
        // automatically — sync them now and whenever the theme changes.
        UpdateTitleBarButtonColors();
        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, _) => UpdateTitleBarButtonColors();

        // Load default page
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    /// <summary>
    /// Applies the saved position/size/maximized-state, if any, and wires up
    /// tracking so the current geometry can be saved again on Closing.
    /// Called from the constructor — i.e. before the first Activate() —
    /// which is safe for Move/Resize (Maximize() is deferred, see below).
    /// </summary>
    private void RestoreWindowGeometry()
    {
        var s = _settingsService.Current;

        if (s.WindowWidth is int w && s.WindowHeight is int h)
        {
            var x = s.WindowX ?? 0;
            var y = s.WindowY ?? 0;

            // Guard against settings from a monitor configuration that no
            // longer exists (laptop undocked, monitor removed/resolution
            // changed) — keep the SDK's default position/size instead of
            // moving the window off-screen where the user can't reach it.
            if (IsRectVisibleOnAnyDisplay(x, y, w, h))
            {
                AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
                _lastRestoredBounds = new RectInt32(x, y, w, h);

                // Maximize() can crash ("Layout Cycle Detected") if called
                // before the window has completed its first Activate() —
                // defer it to the next dispatcher cycle instead.
                if (s.WindowIsMaximized)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (AppWindow.Presenter is OverlappedPresenter presenter)
                            presenter.Maximize();
                    });
                }
            }
        }

        // Keep _lastRestoredBounds in sync while the window is in its
        // normal (restored) state, so Closing always has a sane rect to
        // save even if the user is maximized/minimized at exit time.
        AppWindow.Changed += (sender, _) =>
        {
            if (sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
                _lastRestoredBounds = new RectInt32(
                    sender.Position.X, sender.Position.Y, sender.Size.Width, sender.Size.Height);
        };

        AppWindow.Closing += OnAppWindowClosing;
    }

    /// <summary>
    /// True if the given rect's center point actually lands inside some
    /// display's work area. DisplayArea.GetFromPoint(..., Nearest) always
    /// returns *some* display (never null), so a plain null-check can't
    /// detect "this monitor doesn't exist anymore" — we have to verify the
    /// nearest display's work area genuinely contains the point.
    /// </summary>
    private static bool IsRectVisibleOnAnyDisplay(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return false;

        var center = new PointInt32(x + width / 2, y + height / 2);
        var area = DisplayArea.GetFromPoint(center, DisplayAreaFallback.Nearest);
        if (area is null) return false;

        var wa = area.WorkArea;
        return center.X >= wa.X && center.X < wa.X + wa.Width
            && center.Y >= wa.Y && center.Y < wa.Y + wa.Height;
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var isMaximized = sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Maximized };

        // Prefer the live (restored) rect; fall back to whatever we have
        // tracked if the window happens to be maximized/minimized right now.
        var bounds = (!isMaximized && sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
            ? new RectInt32(sender.Position.X, sender.Position.Y, sender.Size.Width, sender.Size.Height)
            : _lastRestoredBounds;

        if (bounds is { } b)
        {
            var updated = _settingsService.Current with
            {
                WindowX             = b.X,
                WindowY             = b.Y,
                WindowWidth         = b.Width,
                WindowHeight        = b.Height,
                WindowIsMaximized   = isMaximized
            };

            // Fire-and-forget is fine here: SaveAsync writes a small JSON
            // file and the process is about to exit either way; awaiting
            // would require a Deferral, which isn't needed for this.
            _ = _settingsService.SaveAsync(updated);
        }
    }

    private void UpdateTitleBarButtonColors()
    {
        if (Content is not FrameworkElement root) return;
        var isDark = root.ActualTheme == ElementTheme.Dark;

        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonForegroundColor          = isDark ? Colors.White : Colors.Black;
        titleBar.ButtonBackgroundColor          = Colors.Transparent;
        titleBar.ButtonHoverForegroundColor     = isDark ? Colors.White : Colors.Black;
        titleBar.ButtonHoverBackgroundColor     = isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
        titleBar.ButtonPressedForegroundColor   = isDark ? Colors.White : Colors.Black;
        titleBar.ButtonPressedBackgroundColor   = isDark ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0);
        titleBar.ButtonInactiveForegroundColor  = isDark ? Colors.Gray  : Colors.Gray;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();

        var pageType = tag switch
        {
            "Dashboard"   => typeof(DashboardPage),
            "AgentStatus" => typeof(AgentStatusPage),
            "Hardware"    => typeof(HardwarePage),
            "Software"    => typeof(SoftwarePage),
            "Actions"     => typeof(ActionsPage),
            "SoftwareCenter" => typeof(SoftwareCenterPage),
            "Updates"     => typeof(UpdatesPage),
            "Tools"       => typeof(ToolsPage),
            "Console"     => typeof(ConsolePage),
            "Logs"        => typeof(LogsPage),
            _             => typeof(DashboardPage)
        };

        ContentFrame.Navigate(pageType);
    }
}
