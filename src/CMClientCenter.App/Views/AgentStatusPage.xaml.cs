using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace CMClientCenter.App.Views;

public sealed partial class AgentStatusPage : Page
{
    public AgentHealthViewModel ViewModel { get; } =
        App.Services.GetRequiredService<AgentHealthViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public AgentStatusPage()
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
                    case nameof(AgentHealthViewModel.IsLoading):
                        LoadingBar.Visibility = ViewModel.IsLoading
                            ? Visibility.Visible : Visibility.Collapsed;
                        RefreshButton.IsEnabled = !ViewModel.IsLoading;
                        break;

                    case nameof(AgentHealthViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;

                    case nameof(AgentHealthViewModel.Checks):
                        UpdateUI();
                        break;
                }
            });
        };

        _connectionService.ConnectionStateChanged += OnConnectionChanged;

        Loaded += async (_, _) =>
        {
            if (_connectionService.IsConnected)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        };

        Unloaded += (_, _) =>
            _connectionService.ConnectionStateChanged -= OnConnectionChanged;
    }

    private async void OnConnectionChanged(object? sender, ConnectionResult r)
    {
        if (r.IsConnected)
        {
            await Task.Delay(400);
            await ViewModel.RefreshCommand.ExecuteAsync(null);
        }
        else
        {
            _dispatcher.TryEnqueue(() =>
            {
                ChecksPanel.Children.Clear();
                HealthyCount.Text = WarningCount.Text = ErrorCount.Text = "0";
            });
        }
    }

    private void UpdateUI()
    {
        HealthyCount.Text = ViewModel.HealthyCount.ToString();
        WarningCount.Text = ViewModel.WarningCount.ToString();
        ErrorCount.Text   = ViewModel.ErrorCount.ToString();

        ChecksPanel.Children.Clear();

        foreach (var group in ViewModel.GroupedChecks)
        {
            // Kategorie-Header
            var header = new TextBlock
            {
                Text  = group.Key,
                Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
                Margin = new Thickness(0, 8, 0, 4)
            };
            ChecksPanel.Children.Add(header);

            // Card mit allen Checks der Kategorie
            var card = new Border
            {
                Background    = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush   = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius  = new CornerRadius(8),
                Padding       = new Thickness(0)
            };

            var stack = new StackPanel();
            var items = group.ToList();

            for (int i = 0; i < items.Count; i++)
            {
                var check = items[i];
                var row   = BuildCheckRow(check);

                // Trennlinie außer bei letztem Element
                if (i < items.Count - 1)
                {
                    var border = new Border
                    {
                        Child           = row,
                        BorderBrush     = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    stack.Children.Add(border);
                }
                else
                {
                    stack.Children.Add(row);
                }
            }

            card.Child = stack;
            ChecksPanel.Children.Add(card);
        }
    }

    private static Grid BuildCheckRow(HealthCheck check)
    {
        var grid = new Grid { Padding = new Thickness(16, 10, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Status-Indikator
        var dot = new Ellipse
        {
            Width  = 10,
            Height = 10,
            Fill   = StatusColor(check.Status),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(dot, 0);

        // Name
        var name = new TextBlock
        {
            Text = check.Name,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(name, 1);

        // Value + Detail
        var valuePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var value = new TextBlock { Text = check.Value };
        valuePanel.Children.Add(value);

        if (!string.IsNullOrEmpty(check.Detail))
        {
            var detail = new TextBlock
            {
                Text     = check.Detail,
                FontSize = 11,
                Opacity  = 0.6
            };
            valuePanel.Children.Add(detail);
        }
        Grid.SetColumn(valuePanel, 2);

        // Status-Label
        var statusLabel = new Border
        {
            Background    = StatusColor(check.Status),
            CornerRadius  = new CornerRadius(4),
            Padding       = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        var statusText = new TextBlock
        {
            Text       = check.Status,
            Foreground = new SolidColorBrush(Colors.White),
            FontSize   = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        statusLabel.Child = statusText;
        Grid.SetColumn(statusLabel, 3);

        grid.Children.Add(dot);
        grid.Children.Add(name);
        grid.Children.Add(valuePanel);
        grid.Children.Add(statusLabel);

        return grid;
    }

    private static SolidColorBrush StatusColor(string status) => status switch
    {
        "Healthy" => new SolidColorBrush(Colors.ForestGreen),
        "Warning" => new SolidColorBrush(Colors.DarkOrange),
        "Error"   => new SolidColorBrush(Colors.Crimson),
        _         => new SolidColorBrush(Colors.Gray)
    };
}
