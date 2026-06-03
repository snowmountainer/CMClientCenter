using CMClientCenter.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        // Standard-Seite laden
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];
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
            "Tools"       => typeof(ToolsPage),
            "Logs"        => typeof(LogsPage),
            _             => typeof(DashboardPage)
        };

        ContentFrame.Navigate(pageType);
    }
}
