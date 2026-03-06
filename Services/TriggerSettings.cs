using System.IO;
using System.Text.Json;

namespace VXTrigger.Services;

public class TriggerSettings
{
    // Shots directory to monitor
    public string ShotsDirectoryPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProTeeUnited", "Shots");

    // Audio trigger
    public bool AudioTriggerEnabled { get; set; }
    public int SelectedDeviceIndex { get; set; } = -1;
    public string? SelectedDeviceName { get; set; }

    // Tone envelope parameters
    public double ToneFrequencyHz { get; set; } = 5800;
    public double ToneNoiseDecay { get; set; } = 60;
    public double ToneToneDecay { get; set; } = 200;
    public double ToneMix { get; set; } = 0.1;
    public double ToneDurationMs { get; set; } = 500;

    // Network trigger
    public bool NetworkTriggerEnabled { get; set; }
    public int NetworkTriggerPort { get; set; } = 8875;
    public string NetworkTriggerHost { get; set; } = "127.0.0.1";

    // Swing video capture
    public bool SwingVideoEnabled { get; set; }
    public string SwingVideoSourcePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProTeeUnited", "SwingVideos");
    public string SwingVideoDestinationPath { get; set; } = "";

    private static string SettingsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "VXTrigger");

    private static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static TriggerSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<TriggerSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }

        // Try importing from SimLogger settings on first launch
        var imported = TryImportFromSimLogger();
        if (imported != null)
        {
            imported.Save();
            return imported;
        }

        return new TriggerSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    private static TriggerSettings? TryImportFromSimLogger()
    {
        try
        {
            var simLoggerSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SimLogger", "settings.json");

            if (!File.Exists(simLoggerSettingsPath))
                return null;

            var json = File.ReadAllText(simLoggerSettingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var settings = new TriggerSettings();

            if (root.TryGetProperty("AudioTriggerEnabled", out var audioEnabled))
                settings.AudioTriggerEnabled = audioEnabled.GetBoolean();
            if (root.TryGetProperty("SelectedDeviceIndex", out var deviceIndex))
                settings.SelectedDeviceIndex = deviceIndex.GetInt32();
            if (root.TryGetProperty("SelectedDeviceName", out var deviceName))
                settings.SelectedDeviceName = deviceName.GetString();
            if (root.TryGetProperty("ToneFrequencyHz", out var freq) && freq.GetDouble() > 0)
                settings.ToneFrequencyHz = freq.GetDouble();
            if (root.TryGetProperty("ToneNoiseDecay", out var nd) && nd.GetDouble() > 0)
                settings.ToneNoiseDecay = nd.GetDouble();
            if (root.TryGetProperty("ToneToneDecay", out var td) && td.GetDouble() > 0)
                settings.ToneToneDecay = td.GetDouble();
            if (root.TryGetProperty("ToneMix", out var mix) && mix.GetDouble() > 0)
                settings.ToneMix = mix.GetDouble();
            if (root.TryGetProperty("ToneDurationMs", out var dur) && dur.GetDouble() > 0)
                settings.ToneDurationMs = dur.GetDouble();
            if (root.TryGetProperty("NetworkTriggerEnabled", out var netEnabled))
                settings.NetworkTriggerEnabled = netEnabled.GetBoolean();
            if (root.TryGetProperty("NetworkTriggerPort", out var netPort))
                settings.NetworkTriggerPort = netPort.GetInt32();
            if (root.TryGetProperty("NetworkTriggerHost", out var netHost))
                settings.NetworkTriggerHost = netHost.GetString() ?? "127.0.0.1";

            return settings;
        }
        catch
        {
            return null;
        }
    }
}
