using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace CMClientCenter.App.Views;

public sealed partial class ActionsPage : Page
{
    public ActionsViewModel ViewModel { get; } =
        App.Services.GetRequiredService<ActionsViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly StringBuilder _log = new();

    public ActionsPage()
    {
        InitializeComponent();

        ActionsList.ItemsSource = ViewModel.Actions;
        AdvancedActionsList.ItemsSource = ViewModel.AdvancedActions;

        UpdateConnectionState(_connectionService.IsConnected);
        _connectionService.ConnectionStateChanged += OnConnectionChanged;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ActionsViewModel.LastResult))
                _dispatcher.TryEnqueue(() => ShowResult(ViewModel.LastResult));
        };

        ClearLogButton.Click += (_, _) =>
        {
            _log.Clear();
            LogText.Text = "";
        };

        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionChanged;
    }

    private void OnConnectionChanged(object? sender, ConnectionResult r)
        => _dispatcher.TryEnqueue(() => UpdateConnectionState(r.IsConnected));

    private void UpdateConnectionState(bool connected)
    {
        NotConnectedBar.IsOpen  = !connected;
        ActionsList.IsEnabled   = connected;
        AdvancedActionsList.IsEnabled = connected;
    }

    private async void ActionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CMAction action) return;

        try
        {
            ActionsList.IsEnabled = false;
            ResultBar.IsOpen      = false;
            AddLog($"{DateTime.Now:HH:mm:ss}  ▶ {action.Name}…");

            await ViewModel.TriggerActionCommand.ExecuteAsync(action);
        }
        catch (Exception ex)
        {
            AddLog($"{DateTime.Now:HH:mm:ss}  ✗ Unexpected error: {ex.Message}");
        }
        finally
        {
            // Ensure we're back on the UI thread
            _dispatcher.TryEnqueue(() =>
            {
                ActionsList.IsEnabled = _connectionService.IsConnected;
                AdvancedActionsList.IsEnabled = _connectionService.IsConnected;
            });
        }
    }

    private void ShowResult(string? result)
    {
        if (result is null) return;

        var success      = result.StartsWith("✓");
        ResultBar.Severity = success
            ? InfoBarSeverity.Success
            : InfoBarSeverity.Error;
        ResultBar.Message = result;
        ResultBar.IsOpen  = true;

        AddLog($"{DateTime.Now:HH:mm:ss}  {result}");
    }

    private void AddLog(string line)
    {
        _log.Insert(0, line + Environment.NewLine);
        var lines = _log.ToString().Split(Environment.NewLine, StringSplitOptions.None);
        if (lines.Length > 50)
        {
            _log.Clear();
            _log.Append(string.Join(Environment.NewLine, lines.Take(50)));
        }
        LogText.Text = _log.ToString();
    }
}
