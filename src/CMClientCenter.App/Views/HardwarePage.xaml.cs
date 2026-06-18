using CMClientCenter.App.ViewModels;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CMClientCenter.App.Views;

public sealed partial class HardwarePage : Page
{
    public HardwareViewModel ViewModel { get; } =
        App.Services.GetRequiredService<HardwareViewModel>();

    private readonly IConnectionService _connectionService =
        App.Services.GetRequiredService<IConnectionService>();

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public HardwarePage()
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
                    case nameof(HardwareViewModel.IsLoading):
                        LoadingBar.Visibility   = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                        RefreshButton.IsEnabled = !ViewModel.IsLoading;
                        break;
                    case nameof(HardwareViewModel.ErrorMessage):
                        ErrorBar.IsOpen  = ViewModel.ErrorMessage is not null;
                        ErrorBar.Message = ViewModel.ErrorMessage ?? "";
                        break;
                    case nameof(HardwareViewModel.HardwareInfo):
                        BuildUI(ViewModel.HardwareInfo);
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
        if (r.IsConnected) { await Task.Delay(400); await ViewModel.RefreshCommand.ExecuteAsync(null); }
        else _dispatcher.TryEnqueue(() => ContentPanel.Children.Clear());
    }

    private void BuildUI(HardwareInfo? h)
    {
        ContentPanel.Children.Clear();
        if (h is null) return;

        // System
        AddCard("System", [
            ("Manufacturer",  h.Manufacturer),
            ("Model",         h.Model),
            ("Serial Number", h.SerialNumber),
            ("BIOS Version",  h.BIOSVersion),
            ("BIOS Date",     h.BIOSDate),
        ]);

        // CPU
        AddCard("Processor", [
            ("Name",            h.CPUName),
            ("Cores / Threads", $"{h.CPUCores} cores / {h.CPULogical} threads"),
            ("Socket",          h.CPUSocket),
            ("Max. Clock",      h.CPUMaxMHz > 0 ? $"{h.CPUMaxMHz} MHz" : "-"),
        ]);

        // RAM
        var ramRows = new List<(string, string)>
        {
            ("Total", $"{h.TotalRAMGB} GB")
        };
        foreach (var slot in h.RAMSlots)
            ramRows.Add(($"Slot {slot.Slot}", $"{slot.SizeGB} GB  {slot.SpeedMHz} MHz  {slot.Manufacturer}".Trim()));
        AddCard("Memory", ramRows);

        // GPU
        if (!string.IsNullOrEmpty(h.GPUName))
            AddCard("Graphics", [
                ("GPU",    h.GPUName),
                ("VRAM",   h.GPUVRAMMB > 0 ? $"{h.GPUVRAMMB} MB" : "-"),
            ]);

        // Operating System
        AddCard("Operating System", [
            ("Name",         h.OSCaption),
            ("Build",        h.OSBuild),
            ("Architecture", h.OSArch),
            ("Installed",    h.OSInstall),
            ("Last Boot",    h.LastBoot),
        ]);

        // Drives
        if (h.Disks.Count > 0)
        {
            var diskRows = new List<(string, string)>();
            foreach (var d in h.Disks)
            {
                var label = string.IsNullOrEmpty(d.Label) ? d.DriveLetter : $"{d.DriveLetter} ({d.Label})";
                diskRows.Add((label, $"{d.FreeGB} GB free of {d.TotalGB} GB ({d.FreePct}%)  [{d.FileSystem}]"));
            }
            AddCardWithBars("Drives", h.Disks);
        }

        // Network
        if (h.NICs.Count > 0)
        {
            var nicRows = new List<(string, string)>();
            foreach (var n in h.NICs)
                nicRows.Add((n.Description.Length > 35 ? n.Description[..35] + "…" : n.Description,
                    $"{n.IPAddress}  {n.MACAddress}"));
            AddCard("Network", nicRows);
        }
    }

    private void AddCard(string title, IEnumerable<(string Key, string Value)> rows)
    {
        var card  = MakeCard(title);
        var stack = (StackPanel)((Border)card.Child).Child;

        foreach (var (key, val) in rows)
        {
            if (string.IsNullOrWhiteSpace(val) || val == "0") continue;
            var grid = MakeRow(key, val);
            stack.Children.Add(grid);
        }

        ContentPanel.Children.Add(card);
    }

    private void AddCardWithBars(string title, List<DiskInfo> disks)
    {
        var card  = MakeCard(title);
        var stack = (StackPanel)((Border)card.Child).Child;

        foreach (var d in disks)
        {
            var label = string.IsNullOrEmpty(d.Label) ? d.DriveLetter : $"{d.DriveLetter}  {d.Label}";
            var info  = $"{d.FreeGB} GB free  /  {d.TotalGB} GB  [{d.FileSystem}]";

            var col = new StackPanel { Margin = new Thickness(0, 6, 0, 6), Spacing = 4 };

            var hdr = new Grid();
            hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblDrive = new TextBlock { Text = label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            var lblInfo  = new TextBlock
            {
                Text       = info,
                FontSize   = 11,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            var lblPct   = new TextBlock
            {
                Text              = $"{d.FreePct}% free",
                FontSize          = 11,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(lblDrive, 0);
            Grid.SetColumn(lblInfo,  1);
            Grid.SetColumn(lblPct,   2);
            hdr.Children.Add(lblDrive);
            hdr.Children.Add(lblInfo);
            hdr.Children.Add(lblPct);
            col.Children.Add(hdr);

            // Progress bar (used = red/orange/green)
            var bar = new ProgressBar
            {
                Value   = 100 - d.FreePct,
                Maximum = 100,
                Height  = 6
            };
            col.Children.Add(bar);
            stack.Children.Add(col);
        }

        ContentPanel.Children.Add(card);
    }

    private static Border MakeCard(string title)
    {
        var inner = new StackPanel { Spacing = 0 };
        inner.Children.Add(new TextBlock
        {
            Text   = title,
            Style  = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            Margin = new Thickness(0, 0, 0, 8)
        });

        return new Border
        {
            Background      = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush     = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(16),
            Child           = new Border { Child = inner }   // wrapper so we can get stack later
        };
    }

    private static Grid MakeRow(string key, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var keyTb = new TextBlock
        {
            Text       = key,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };
        var valTb = new TextBlock
        {
            Text         = value,
            TextWrapping = TextWrapping.Wrap
        };

        Grid.SetColumn(keyTb, 0);
        Grid.SetColumn(valTb, 1);
        grid.Children.Add(keyTb);
        grid.Children.Add(valTb);
        return grid;
    }
}
