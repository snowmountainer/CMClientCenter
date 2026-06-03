using CMClientCenter.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace CMClientCenter.App.Controls;

public sealed partial class ConnectionBar : UserControl
{
    public MainViewModel ViewModel { get; } =
        App.Services.GetRequiredService<MainViewModel>();

    public ConnectionBar()
    {
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsConnected):
                    UpdateConnectionState();
                    break;
                case nameof(MainViewModel.ErrorMessage):
                    // Fehler sichtbar anzeigen
                    if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
                    {
                        ErrorBar.Message = ViewModel.ErrorMessage;
                        ErrorBar.IsOpen  = true;
                    }
                    else
                    {
                        ErrorBar.IsOpen = false;
                    }
                    break;
            }
        };
    }

    private void UpdateConnectionState()
    {
        var connected = ViewModel.IsConnected;
        DisconnectButton.Visibility = connected
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        StatusDot.Fill  = new SolidColorBrush(connected ? Colors.LimeGreen : Colors.Gray);
        ErrorBar.IsOpen = false; // Fehler ausblenden bei Erfolg
    }

    private void HostBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.ConnectCommand.CanExecute(null))
            ViewModel.ConnectCommand.Execute(null);
    }
}
