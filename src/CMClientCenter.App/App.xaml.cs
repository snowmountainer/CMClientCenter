using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.Core.Services;
using CMClientCenter.PowerShell.Engine;
using CMClientCenter.PowerShell.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace CMClientCenter.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window? MainAppWindow { get; private set; }

    public App()
    {
        this.UnhandledException += OnUnhandledException;
        InitializeComponent();
        try
        {
            Services = ConfigureServices();

            // Application.RequestedTheme must be set before the first Activate() —
            // it controls how ThemeResource brushes in Application.Resources
            // resolve (e.g. CardBackgroundFillColorDefaultBrush). Setting only
            // Window.Content.RequestedTheme later is not enough: that covers
            // FrameworkElement-level theme but leaves Application-level brushes
            // on their initial resolution.
            var settingsService = Services.GetRequiredService<IAppSettingsService>();
            this.RequestedTheme = ToApplicationTheme(settingsService.Current.Theme);
        }
        catch (Exception ex) { LogCrash("DI-Setup", ex); throw; }
    }

    /// <summary>
    /// Application.RequestedTheme only knows Light/Dark (no "System" value),
    /// so for AppTheme.System we resolve the current Windows theme once here
    /// by checking the system's background color brightness.
    /// </summary>
    private static ApplicationTheme ToApplicationTheme(Shared.Enums.AppTheme theme)
    {
        if (theme == Shared.Enums.AppTheme.Light) return ApplicationTheme.Light;
        if (theme == Shared.Enums.AppTheme.Dark)  return ApplicationTheme.Dark;

        // AppTheme.System: ask Windows directly
        var bg = new Windows.UI.ViewManagement.UISettings()
            .GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var isDark = (bg.R + bg.G + bg.B) < 384; // roughly black vs. white
        return isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => { b.AddDebug(); b.SetMinimumLevel(LogLevel.Debug); });

        // PowerShell Engine
        services.AddSingleton<RunspaceManager>();
        services.AddSingleton<IRunspaceManagerService>(sp => sp.GetRequiredService<RunspaceManager>());

        // Executors
        services.AddSingleton<CMAgentExecutor>();
        services.AddSingleton<IExecutorService<CMAgentInfo>>(sp => sp.GetRequiredService<CMAgentExecutor>());
        services.AddSingleton<HardwareExecutor>();
        services.AddSingleton<IExecutorService<HardwareInfo>>(sp => sp.GetRequiredService<HardwareExecutor>());
        services.AddSingleton<SoftwareExecutor>();
        services.AddSingleton<IExecutorService<List<SoftwareItem>>>(sp => sp.GetRequiredService<SoftwareExecutor>());
        services.AddSingleton<ActionExecutor>();
        services.AddSingleton<IActionExecutorService>(sp => sp.GetRequiredService<ActionExecutor>());
        services.AddSingleton<HealthExecutor>();
        services.AddSingleton<IHealthExecutorService>(sp => sp.GetRequiredService<HealthExecutor>());
        services.AddSingleton<LogExecutor>();
        services.AddSingleton<ToolsExecutor>();
        services.AddSingleton<SoftwareCenterExecutor>();
        services.AddSingleton<UpdatesExecutor>();

        // Core Services
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<ICMAgentService, CMAgentService>();
        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<ISoftwareService, SoftwareService>();
        services.AddSingleton<IActionService, ActionService>();
        services.AddSingleton<IAgentHealthService, AgentHealthService>();
        services.AddSingleton<ILogService, CMClientCenter.PowerShell.Executors.LogService>();
        services.AddSingleton<IToolsService>(sp => sp.GetRequiredService<ToolsExecutor>());
        services.AddSingleton<ISoftwareCenterService>(sp => sp.GetRequiredService<SoftwareCenterExecutor>());
        services.AddSingleton<IUpdatesService>(sp => sp.GetRequiredService<UpdatesExecutor>());

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AgentHealthViewModel>();
        services.AddTransient<HardwareViewModel>();
        services.AddTransient<SoftwareViewModel>();
        services.AddTransient<ActionsViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<ToolsViewModel>();
        services.AddTransient<SoftwareCenterViewModel>();
        services.AddTransient<UpdatesViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        catch (Exception ex) { LogCrash("OnLaunched", ex); }
    }

    /// <summary>
    /// Applies the chosen theme to the given window's root element.
    /// NOTE: Application.RequestedTheme (which controls ThemeResource
    /// brushes in Application.Resources, e.g. card backgrounds) can only
    /// be set once, before the very first Window.Activate() — see the
    /// App() constructor. Setting it again later throws a COMException.
    /// So at runtime we can only update the window-level (FrameworkElement)
    /// theme; Application-level brushes require a restart to fully follow
    /// a new theme choice.
    /// </summary>
    public static void ApplyTheme(Window window, Shared.Enums.AppTheme theme)
    {
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                Shared.Enums.AppTheme.Light => ElementTheme.Light,
                Shared.Enums.AppTheme.Dark  => ElementTheme.Dark,
                _                           => ElementTheme.Default
            };
        }
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        LogCrash("UnhandledException", e.Exception);
    }

    private static void LogCrash(string context, Exception ex)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "CMClientCenter_crash.txt");
        File.WriteAllText(path,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\r\n" +
            $"Type:    {ex.GetType().FullName}\r\n" +
            $"Message: {ex.Message}\r\n" +
            $"Inner:   {ex.InnerException?.Message}\r\n\r\n" +
            $"Stack:\r\n{ex.StackTrace}\r\n");
    }
}
