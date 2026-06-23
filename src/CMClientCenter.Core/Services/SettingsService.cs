using System.Text.Json;
using System.Text.Json.Serialization;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.Core.Services;

/// <summary>
/// Persists application settings (theme, etc.) as JSON under
/// %LOCALAPPDATA%\CMClientCenter\settings.json.
/// The app runs unpackaged (WindowsPackageType=None), so the WinRT
/// ApplicationData API is not reliably available — a plain file is the
/// simplest, most portable choice (works xcopy/portable too).
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;
    private readonly string _defaultScriptsFolder;
    private readonly ILogger<AppSettingsService> _logger;

    public AppSettings Current { get; private set; }

    public string EffectiveScriptsFolder =>
        string.IsNullOrWhiteSpace(Current.ScriptsFolder) ? _defaultScriptsFolder : Current.ScriptsFolder;

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettingsService(ILogger<AppSettingsService> logger)
    {
        _logger = logger;

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CMClientCenter");

        _settingsPath         = Path.Combine(folder, "settings.json");
        _defaultScriptsFolder = Path.Combine(folder, "Scripts");
        Current                = Load(folder);
    }

    private AppSettings Load(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);

            if (File.Exists(_settingsPath))
            {
                var json     = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null) return settings;
            }
        }
        catch (Exception ex)
        {
            // Corrupt or unreadable settings file — fall back to defaults rather than crash the app
            _logger.LogWarning(ex, "Failed to load settings from {Path}, using defaults", _settingsPath);
        }

        return new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json, ct);
            Current = settings;
            SettingsChanged?.Invoke(this, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsPath);
        }
    }
}
