using System.Text.Json;

namespace Nuri.SimplyShare.Features.Settings;

public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nuri.SimplyShare",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), JsonOptions)
                    ?? AppSettings.CreateDefault();
        }
        catch
        {
        }

        return AppSettings.CreateDefault();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        Directory.CreateDirectory(settings.DownloadPath);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
