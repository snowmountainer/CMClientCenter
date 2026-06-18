using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App.Controls;

public sealed partial class CredentialsDialog : ContentDialog
{
    public string Username => UsernameBox.Text.Trim();
    public string Password => PasswordBox.Password;

    public CredentialsDialog(string hostname)
    {
        InitializeComponent();
        HostInfo.Text = $"Connecting to: {hostname}";
    }
}
