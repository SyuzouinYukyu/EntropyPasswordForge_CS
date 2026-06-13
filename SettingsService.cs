using System.Text.Json;

namespace EntropyPasswordForge_CS;

internal sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string SettingsPath { get; } = Path.Combine(GetBaseSettingsFolder(), "EntropyPasswordForge_CS", "settings.json");

    private static string GetBaseSettingsFolder()
    {
        string? overrideFolder = Environment.GetEnvironmentVariable("ENTROPY_PASSWORD_FORGE_SETTINGS_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideFolder) && Path.IsPathRooted(overrideFolder))
        {
            return overrideFolder;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public bool TryLoad(out AppSettings settings)
    {
        settings = new AppSettings();
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return true;
            }

            string json = File.ReadAllText(SettingsPath);
            AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            settings = loaded ?? new AppSettings();
            return true;
        }
        catch
        {
            settings = new AppSettings();
            return false;
        }
    }

    public bool TrySave(AppSettings settings)
    {
        try
        {
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
