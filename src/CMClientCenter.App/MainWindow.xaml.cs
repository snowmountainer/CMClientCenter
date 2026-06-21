using CMClientCenter.App.Views;
using CMClientCenter.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace CMClientCenter.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        // Apply the persisted theme BEFORE navigating to the first page.
        // Pages resolve ThemeResource bindings at load time — navigating
        // first and setting the theme afterwards leaves already-loaded
        // pages stuck on the wrong theme even though RequestedTheme changes.
        // Note: pass 'this' explicitly — App.MainAppWindow is still null
        // at this point because 'new MainWindow()' hasn't returned yet.
        var settingsService = App.Services.GetRequiredService<IAppSettingsService>();
        App.ApplyTheme(this, settingsService.Current.Theme);
        settingsService.SettingsChanged += (_, settings) =>
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
            "Logs"        => typeof(LogsPage),
            _             => typeof(DashboardPage)
        };

        ContentFrame.Navigate(pageType);
    }
}
