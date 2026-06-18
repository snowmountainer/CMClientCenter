using CMClientCenter.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

namespace CMClientCenter.App.ViewModels;

public partial class MainViewModel(IConnectionService connectionService) : ObservableObject
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    public partial string TargetHost { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsConnected { get; set; }
    [ObservableProperty] public partial string ConnectionStatus { get; set; } = "Not connected";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    public partial bool IsConnecting { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    // Credentials for remote connections
    public string? Username { get; set; }
    public string? Password { get; set; }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetHost)) return;
        IsConnecting = true;
        ErrorMessage = null;

        // Only pass username/password for remote connections
        var username = string.IsNullOrEmpty(Username) ? null : Username;
        var password = string.IsNullOrEmpty(Password) ? null : Password;

        var result = await connectionService.ConnectAsync(TargetHost.Trim(), username, password);

        _dispatcher.TryEnqueue(() =>
        {
            if (result.IsSuccess && result.Value is { } r)
            {
                IsConnected      = true;
                ConnectionStatus = $"{(r.Mode == Shared.Enums.ConnectionMode.Local ? "Local" : "Remote")} — {r.OSVersion ?? "connected"}";
            }
            else
            {
                IsConnected      = false;
                ConnectionStatus = "Connection error";
                ErrorMessage     = result.ErrorMessage;
            }
            IsConnecting = false;
        });
    }

    private bool CanConnect() => !IsConnecting && !string.IsNullOrWhiteSpace(TargetHost);

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await connectionService.DisconnectAsync();
        _dispatcher.TryEnqueue(() =>
        {
            IsConnected      = false;
            ConnectionStatus = "Not connected";
        });
    }
}
