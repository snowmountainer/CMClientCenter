using CMClientCenter.Core.Interfaces;
using CMClientCenter.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Reflection;

namespace CMClientCenter.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly IAppSettingsService _settingsService =
        App.Services.GetRequiredService<IAppSettingsService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

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

        ScriptsFolderBox.Text = _settingsService.EffectiveScriptsFolder;
        BrowseScriptsFolderButton.Click += async (_, _) => await BrowseForScriptsFolderAsync();

        InitializeAboutSection();
    }

    /// <summary>
    /// Reads the version from the executing assembly instead of hardcoding a
    /// string here, so this box never goes stale after a release bump.
    ///
    /// Uses InformationalVersion ("1.0.0+a1b2c3d[-dirty]") rather than
    /// GetName().Version ("1.0.0.0") because the latter never carries the git
    /// hash — SourceRevisionId (set in Directory.Build.props from `git
    /// rev-parse`) only flows into InformationalVersion, not AssemblyVersion.
    /// That's exactly what we want here: two people comparing "1.0.0" alone
    /// can't tell if they're on the same commit; the hash makes that
    /// unambiguous when reporting a bug or comparing builds.
    /// </summary>
    private void InitializeAboutSection()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var version = assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var commitHash = informational?.Contains('+') == true
            ? informational[(informational.IndexOf('+') + 1)..]
            : null;

        AboutAppNameText.Text = "CMClientCenter";
        AboutVersionText.Text = string.IsNullOrEmpty(commitHash)
            ? $"Version {version}"
            : $"Version {version} ({commitHash})";
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
        //
        // The continuation after 'await' on file I/O is NOT guaranteed to resume
        // on the UI thread in WinUI 3 (no automatic SynchronizationContext capture
        // like WPF/WinForms) — touching RestartBar directly here crashed on some
        // machines even though it worked on the dev box. Always dispatch explicitly.
        _dispatcher.TryEnqueue(() =>
        {
            RestartBar.IsOpen = true;
            RestartBarCloseButton.Visibility = Visibility.Visible;
        });
    }

    /// <summary>
    /// Lets the user pick the folder scanned by the Console page's "Run PS"
    /// list. Uses Windows.Storage.Pickers.FolderPicker — in an unpackaged
    /// desktop app this requires associating the picker with the current
    /// window's HWND first (WinRT.Interop), unlike in UWP where this is implicit.
    /// </summary>
    private async Task BrowseForScriptsFolderAsync()
    {
        if (App.MainAppWindow is not { } window) return;

        var folderPicker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder
        };
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is null) return; // user cancelled

        var updated = _settingsService.Current with { ScriptsFolder = folder.Path };
        await _settingsService.SaveAsync(updated);

        _dispatcher.TryEnqueue(() => ScriptsFolderBox.Text = _settingsService.EffectiveScriptsFolder);
    }

    private void RestartBar_Close_Click(object sender, RoutedEventArgs e)
    {
        // AppInstance.Restart() is the "official" WinUI 3 restart API, but it has a
        // known issue on unpackaged self-contained apps (FileNotFoundException in
        // WinRT.Runtime.dll) — see github.com/microsoft/WindowsAppSDK-Samples#233.
        // Since this app runs unpackaged (WindowsPackageType=None), we instead spawn
        // a fresh copy of our own executable and exit — works for any .exe, packaged
        // or not, with no WinRT dependency.
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = exePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // If relaunching fails for any reason, at least don't leave the user
            // stuck without explanation — they can still close the InfoBar and
            // restart manually.
        }

        Environment.Exit(0);
    }
}
