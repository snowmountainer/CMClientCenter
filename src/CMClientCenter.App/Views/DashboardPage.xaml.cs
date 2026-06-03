using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CMClientCenter.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; } =
        App.Services.GetRequiredService<DashboardViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public DashboardPage()
    {
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(DashboardViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading
                            ? Microsoft.UI.Xaml.Visibility.Visible
                            : Microsoft.UI.Xaml.Visibility.Collapsed;
                        break;

                    case nameof(DashboardViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;

                    case nameof(DashboardViewModel.AgentInfo):
                        UpdateAgentUI(ViewModel.AgentInfo);
                        break;

                    case nameof(DashboardViewModel.HardwareInfo):
                        UpdateHardwareUI(ViewModel.HardwareInfo);
                        break;
                }
            });
        };

        // ConnectionStateChanged — wird bei jedem Connect/Disconnect gefeuert
        _connectionService.ConnectionStateChanged += OnConnectionStateChanged;

        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        };

        // Aufräumen wenn Page entladen wird
        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionStateChanged;
    }

    private async void OnConnectionStateChanged(object? sender, ConnectionResult connResult)
    {
        if (connResult.IsConnected)
        {
            // Kurz warten bis Runspace vollständig initialisiert ist
            await Task.Delay(300);
            await ViewModel.RefreshCommand.ExecuteAsync(null);
        }
        else
        {
            _dispatcher.TryEnqueue(ResetFields);
        }
    }

    private void UpdateAgentUI(CMAgentInfo? a)
    {
        if (a is null) { ResetAgentFields(); return; }

        if (string.IsNullOrEmpty(a.ClientVersion))
        {
            TxtVersion.Text = "Kein Zugriff";
            DiagBar.IsOpen  = true;
            DiagBar.Message = a.DiagInfo;
        }
        else
        {
            TxtVersion.Text = a.ClientVersion;
            DiagBar.IsOpen  = false;
        }

        TxtClientId.Text = string.IsNullOrEmpty(a.ClientId) ? "-" : a.ClientId;
        TxtSiteCode.Text = string.IsNullOrEmpty(a.SiteCode) ? "-" : a.SiteCode;
        TxtMP.Text       = string.IsNullOrEmpty(a.ManagementPoint) ? "-" : a.ManagementPoint;
        TxtHWInv.Text    = a.LastHardwareInventory?.ToString("dd.MM.yyyy HH:mm") ?? "-";
        TxtCache.Text    = a.CacheSize;

        (StateBadge.Background, StateBadgeText.Text) = a.State switch
        {
            CMClientState.Healthy      => (new SolidColorBrush(Colors.ForestGreen), "Healthy"),
            CMClientState.Warning      => (new SolidColorBrush(Colors.DarkOrange),  "Warning"),
            CMClientState.Error        => (new SolidColorBrush(Colors.Crimson),     "Error"),
            CMClientState.NotInstalled => (new SolidColorBrush(Colors.Gray),        "Nicht installiert"),
            _                          => (new SolidColorBrush(Colors.Gray),        "Unbekannt")
        };
    }

    private void UpdateHardwareUI(HardwareInfo? h)
    {
        if (h is null) { ResetHardwareFields(); return; }
        TxtModel.Text = $"{h.Manufacturer} {h.Model}".Trim();
        TxtCPU.Text   = $"{h.CPUName} ({h.CPUCores} Kerne)";
        TxtRAM.Text   = $"{h.TotalRAMGB} GB";
        TxtOS.Text    = $"{h.OSCaption} (Build {h.OSBuild})";
                            if (!string.IsNullOrEmpty(h.LastBoot)) TxtOS.Text += $"  |  Boot: {h.LastBoot}";
    }

    private void ResetAgentFields()
    {
        TxtVersion.Text = TxtClientId.Text = TxtSiteCode.Text =
        TxtMP.Text = TxtHWInv.Text = TxtCache.Text = "-";
        StateBadgeText.Text       = "?";
        StateBadge.Background     = new SolidColorBrush(Colors.Gray);
        DiagBar.IsOpen            = false;
    }

    private void ResetHardwareFields()
    {
        TxtModel.Text = TxtCPU.Text = TxtRAM.Text = TxtOS.Text = "-";
    }

    private void ResetFields()
    {
        ResetAgentFields();
        ResetHardwareFields();
        ErrorBar.IsOpen = false;
    }
}
