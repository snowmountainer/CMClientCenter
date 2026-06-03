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
        try { Services = ConfigureServices(); }
        catch (Exception ex) { LogCrash("DI-Setup", ex); throw; }
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

        // Core Services
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<ICMAgentService, CMAgentService>();
        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<ISoftwareService, SoftwareService>();
        services.AddSingleton<IActionService, ActionService>();
        services.AddSingleton<IAgentHealthService, AgentHealthService>();
        services.AddSingleton<ILogService, CMClientCenter.PowerShell.Executors.LogService>();
        services.AddSingleton<IToolsService>(sp => sp.GetRequiredService<ToolsExecutor>());

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AgentHealthViewModel>();
        services.AddTransient<HardwareViewModel>();
        services.AddTransient<SoftwareViewModel>();
        services.AddTransient<ActionsViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<ToolsViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try { MainAppWindow = new MainWindow(); MainAppWindow.Activate(); }
        catch (Exception ex) { LogCrash("OnLaunched", ex); }
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
