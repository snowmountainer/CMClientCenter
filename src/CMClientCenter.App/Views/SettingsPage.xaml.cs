using CMClientCenter.Core.Interfaces;
using CMClientCenter.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly IAppSettingsService _settingsService =
        App.Services.GetRequiredService<IAppSettingsService>();

    private bool _suppressSelectionEvent;

    public SettingsPage()
    {
        InitializeComponent();

        // Select the ComboBoxItem matching the currently persisted theme
        // without triggering a redundant save in the SelectionChanged handler.
        _suppressSelectionEvent = true;
        var current = _settingsService.Current.Theme;
        foreach (var obj in ThemeCombo.Items)
        {
            if (obj is ComboBoxItem item && item.Tag?.ToString() == current.ToString())
            {
                ThemeCombo.SelectedItem = obj;
                break;
            }
        }
        _suppressSelectionEvent = false;
    }

    private async void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionEvent) return;
        if (ThemeCombo.SelectedItem is not ComboBoxItem item) return;
        if (!Enum.TryParse<AppTheme>(item.Tag?.ToString(), out var theme)) return;

        var updated = _settingsService.Current with { Theme = theme };
        await _settingsService.SaveAsync(updated);

        // SettingsChanged (raised by SaveAsync) is handled centrally in App.xaml.cs,
        // which updates the window-level theme live. Application-level brushes
        // (card backgrounds etc.) can't be changed after the first Activate() —
        // WinUI throws a COMException if Application.RequestedTheme is set twice.
        // So we're honest with the user instead of silently leaving things half-themed.
        RestartBar.IsOpen = true;
    }
}
