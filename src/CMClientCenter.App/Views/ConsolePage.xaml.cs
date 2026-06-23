using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App.Views;

public sealed partial class ConsolePage : Page
{
    public ConsoleViewModel ViewModel { get; } =
        App.Services.GetRequiredService<ConsoleViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly IAppSettingsService _settingsService =
        App.Services.GetRequiredService<IAppSettingsService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public ConsolePage()
    {
        InitializeComponent();

        ScriptsFolderText.Text = _settingsService.EffectiveScriptsFolder;

        RefreshButton.Click          += async (_, _) => await ViewModel.RefreshScriptsCommand.ExecuteAsync(null);
        OpenConsoleButton.Click      += (_, _) => ViewModel.OpenConsoleCommand.Execute(null);
        ClearOutputButton.Click      += (_, _) => OutputText.Text = "";
        OpenScriptsFolderButton.Click += (_, _) => OpenScriptsFolder();

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ConsoleViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case nameof(ConsoleViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;
                    case nameof(ConsoleViewModel.BuiltinScriptGroups):
                    case nameof(ConsoleViewModel.CustomScriptGroups):
                        UpdateScriptsLists();
                        break;
                    case nameof(ConsoleViewModel.ScriptOutput):
                        if (ViewModel.ScriptOutput is { } output)
                            OutputText.Text = output;
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionChanged;
        UpdateConnectionUI(_connectionService.IsConnected);

        Loaded += async (_, _) => await ViewModel.RefreshScriptsCommand.ExecuteAsync(null);
        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionChanged;
    }

    private void OnConnectionChanged(object? sender, ConnectionResult r) =>
        _dispatcher.TryEnqueue(() => UpdateConnectionUI(r.IsConnected));

    private void UpdateConnectionUI(bool connected)
    {
        NotConnectedBar.IsOpen      = !connected;
        OpenConsoleButton.IsEnabled = connected;
    }

    private void UpdateScriptsLists()
    {
        // CollectionViewSource.Source expects a collection of groups, where
        // each group is itself enumerable (ScriptGroup : List<CustomScriptInfo>
        // already satisfies that) — standard WinUI 3 grouped-ListView pattern.
        BuiltinScriptsGroupedSource.Source = ViewModel.BuiltinScriptGroups;
        BuiltinScriptsList.ItemsSource      = BuiltinScriptsGroupedSource.View;
        NoBuiltinScriptsText.Visibility     = ViewModel.BuiltinScriptGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        CustomScriptsGroupedSource.Source = ViewModel.CustomScriptGroups;
        CustomScriptsList.ItemsSource      = CustomScriptsGroupedSource.View;
        NoCustomScriptsText.Visibility     = ViewModel.CustomScriptGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void RunScript_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CustomScriptInfo script }) return;

        // Built-in scripts can do impactful things (service restarts, WMI repair,
        // etc. — see PSScripts/LICENSE-and-SOURCE.md) and the person running them
        // didn't necessarily write them, unlike their own custom scripts — so they
        // get the same "are you sure" treatment as OSD deployments on the Software
        // Center page (ContentDialog + mandatory confirmation checkbox).
        if (script.IsBuiltin && !await ConfirmBuiltinScriptAsync(script))
            return;

        await ViewModel.RunScriptCommand.ExecuteAsync(script.FullPath);
    }

    private async Task<bool> ConfirmBuiltinScriptAsync(CustomScriptInfo script)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "This is a built-in script that may change settings or restart services " +
                   "on the connected computer. Review the script file first if you're unsure what it does.",
            TextWrapping = TextWrapping.Wrap
        });

        var confirmCheckbox = new CheckBox { Content = "I understand what this script does and want to run it." };
        panel.Children.Add(confirmCheckbox);

        var dialog = new ContentDialog
        {
            Title                  = $"Run \"{script.Name}\"?",
            Content                = panel,
            PrimaryButtonText      = "Run",
            CloseButtonText        = "Cancel",
            DefaultButton          = ContentDialogButton.Close,
            IsPrimaryButtonEnabled = false,
            XamlRoot               = this.XamlRoot
        };

        confirmCheckbox.Checked   += (_, _) => dialog.IsPrimaryButtonEnabled = true;
        confirmCheckbox.Unchecked += (_, _) => dialog.IsPrimaryButtonEnabled = false;

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private void OpenScriptsFolder()
    {
        try
        {
            var folder = _settingsService.EffectiveScriptsFolder;
            System.IO.Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = folder,
                UseShellExecute = true
            });
        }
        catch
        {
            // Non-critical — the path is shown in the UI either way.
        }
    }
}
