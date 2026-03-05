using System.IO;
using ShotTrigger.Services;

namespace ShotTrigger;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--simulate", StringComparer.OrdinalIgnoreCase))
        {
            RunShotSimulator(args);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static void RunShotSimulator(string[] args)
    {
        var settings = TriggerSettings.Load();
        var shotsDir = settings.ShotsDirectoryPath;

        var intervalMs = 5000;
        var intervalArg = args.FirstOrDefault(a => a.StartsWith("--interval=", StringComparison.OrdinalIgnoreCase));
        if (intervalArg != null && int.TryParse(intervalArg.Split('=')[1], out var parsed) && parsed > 0)
            intervalMs = parsed;

        var count = 0;
        var maxShots = 0;
        var countArg = args.FirstOrDefault(a => a.StartsWith("--count=", StringComparison.OrdinalIgnoreCase));
        if (countArg != null && int.TryParse(countArg.Split('=')[1], out var parsedCount) && parsedCount > 0)
            maxShots = parsedCount;

        Console.WriteLine($"Shot Simulator");
        Console.WriteLine($"  Directory: {shotsDir}");
        Console.WriteLine($"  Interval:  {intervalMs}ms");
        Console.WriteLine($"  Count:     {(maxShots > 0 ? maxShots.ToString() : "unlimited")}");
        Console.WriteLine($"  Press Ctrl+C to stop");
        Console.WriteLine();

        if (!Directory.Exists(shotsDir))
        {
            Directory.CreateDirectory(shotsDir);
            Console.WriteLine($"  Created directory: {shotsDir}");
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                count++;
                var folderName = $"SimShot_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
                var folderPath = Path.Combine(shotsDir, folderName);
                Directory.CreateDirectory(folderPath);
                Console.WriteLine($"  [{count}] Created: {folderName}");

                if (maxShots > 0 && count >= maxShots)
                    break;

                Thread.Sleep(intervalMs);
            }
        }
        catch (OperationCanceledException) { }

        Console.WriteLine();
        Console.WriteLine($"  Simulated {count} shot(s).");
    }
}
