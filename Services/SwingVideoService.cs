using System.IO;

namespace VXTrigger.Services;

public class SwingVideoService : IDisposable
{
    private bool _disposed;

    public bool IsEnabled { get; set; }
    public string SourcePath { get; set; } = "";
    public string DestinationPath { get; set; } = "";

    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Called when a shot is detected. Copies any .mp4 files from the source
    /// folder to a date-based subfolder in the destination, with sequential naming.
    /// </summary>
    public void OnShotDetected()
    {
        if (!IsEnabled || string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(DestinationPath))
            return;

        // Run the copy on a background thread to avoid blocking the trigger pipeline
        Task.Run(() => CopySwingVideos());
    }

    private void CopySwingVideos()
    {
        try
        {
            if (!Directory.Exists(SourcePath))
            {
                StatusChanged?.Invoke(this, $"Swing video source not found: {SourcePath}");
                return;
            }

            // Get .mp4 files sorted by last write time (most recent first)
            var mp4Files = new DirectoryInfo(SourcePath)
                .GetFiles("*.mp4")
                .OrderByDescending(f => f.LastWriteTime)
                .ToArray();

            if (mp4Files.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("SwingVideoService: No .mp4 files found in source.");
                return;
            }

            // Create date-based subfolder
            var dateFolder = Path.Combine(DestinationPath, DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dateFolder);

            // Determine next shot number for today's folder
            var shotNum = GetNextShotNumber(dateFolder);

            // Wait briefly for files to finish being written
            Thread.Sleep(1500);

            var copied = 0;
            for (int i = 0; i < mp4Files.Length; i++)
            {
                var src = mp4Files[i];
                try
                {
                    // Refresh file info after the wait
                    src.Refresh();
                    if (!src.Exists) continue;

                    var suffix = mp4Files.Length > 1 ? $"_{i + 1}" : "";
                    var destName = $"Shot_{shotNum:D3}{suffix}.mp4";
                    var destPath = Path.Combine(dateFolder, destName);

                    File.Copy(src.FullName, destPath, overwrite: true);
                    copied++;
                    System.Diagnostics.Debug.WriteLine($"SwingVideoService: Copied {src.Name} -> {destName}");
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SwingVideoService: Failed to copy {src.Name}: {ex.Message}");
                }
            }

            if (copied > 0)
                StatusChanged?.Invoke(this, $"Copied {copied} swing video(s) to {dateFolder}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SwingVideoService: Error: {ex.Message}");
            StatusChanged?.Invoke(this, $"Swing video error: {ex.Message}");
        }
    }

    private static int GetNextShotNumber(string dateFolder)
    {
        var existing = Directory.GetFiles(dateFolder, "Shot_*.mp4");
        int max = 0;
        foreach (var file in existing)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            // Parse "Shot_001" or "Shot_001_1" -> extract 001
            var parts = name.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var num) && num > max)
                max = num;
        }
        return max + 1;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
