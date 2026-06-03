using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace CMClientCenter.App.Views;

public sealed partial class SoftwarePage : Page
{
    public SoftwareViewModel ViewModel { get; } =
        App.Services.GetRequiredService<SoftwareViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private List<SoftwareItem> _allItems = [];

    public SoftwarePage()
    {
        InitializeComponent();

        RefreshButton.Click += async (_, _) =>
            await ViewModel.RefreshCommand.ExecuteAsync(null);

        ViewModel.PropertyChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(SoftwareViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading
                            ? Microsoft.UI.Xaml.Visibility.Visible
                            : Microsoft.UI.Xaml.Visibility.Collapsed;
                        RefreshButton.IsEnabled = !ViewModel.IsLoading;
                        break;

                    case nameof(SoftwareViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;

                    case nameof(SoftwareViewModel.Items):
                        _allItems = ViewModel.Items;
                        ApplyFilter(FilterBox.Text);
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionStateChanged;

        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected && ViewModel.Items.Count == 0)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        };

        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionStateChanged;
    }

    private async void OnConnectionStateChanged(object? sender, ConnectionResult connResult)
    {
        if (connResult.IsConnected)
        {
            await Task.Delay(500); // Warten bis Runspace bereit
            await ViewModel.RefreshCommand.ExecuteAsync(null);
        }
        else
        {
            _dispatcher.TryEnqueue(() =>
            {
                _allItems = [];
                SoftwareList.ItemsSource = null;
                CountText.Text = "0";
                FilterBox.Text = "";
                ErrorBar.IsOpen = false;
            });
        }
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter(FilterBox.Text);

    private void ApplyFilter(string filter)
    {
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allItems
            : _allItems.Where(i =>
                i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                i.Publisher.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                i.Version.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        SoftwareList.ItemsSource = filtered;
        CountText.Text = string.IsNullOrWhiteSpace(filter)
            ? $"{filtered.Count}"
            : $"{filtered.Count} / {_allItems.Count}";
    }
}
