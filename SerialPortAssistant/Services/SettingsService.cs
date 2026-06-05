using System.IO;
using System.Text.Json;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    public AppSettings Current { get; private set; }

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SerialPortAssistant");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
        Current = Load();
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
      settings.LogLevels = LogLevelConfig.Normalize(settings.LogLevels);
      CommandOpcodeCatalog.ApplyMigration(settings);
      return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Current = settings;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    public void Reload() => Current = Load();
}
