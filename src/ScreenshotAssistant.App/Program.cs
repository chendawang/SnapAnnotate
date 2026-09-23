using Avalonia;
using ScreenshotAssistant.Capture;
using System.Text.Json;

namespace ScreenshotAssistant.App;

internal static class Program
{
    private static readonly JsonSerializerOptions DiagnosticsJsonOptions = new()
    {
        WriteIndented = true
    };

    [STAThread]
    public static void Main(string[] args)
    {
        int diagnosticsArgumentIndex = Array.FindIndex(
            args,
            argument => string.Equals(
                argument,
                "--capture-diagnostics",
                StringComparison.OrdinalIgnoreCase));
        if (diagnosticsArgumentIndex >= 0)
        {
            Environment.ExitCode = RunCaptureDiagnostics(args, diagnosticsArgumentIndex);
            return;
        }

        using ShareXSingleInstanceService singleInstanceService = new(args);
        if (!singleInstanceService.IsFirstInstance)
        {
            return;
        }

        App.SingleInstanceService = singleInstanceService;
        App.LaunchInBackground = args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static int RunCaptureDiagnostics(string[] args, int argumentIndex)
    {
        if (argumentIndex + 1 >= args.Length)
        {
            return 2;
        }

        string outputPath = Path.GetFullPath(args[argumentIndex + 1]);
        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        CaptureDiagnosticsReport report = ShareXCaptureDiagnosticsService.Run();
        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(report, DiagnosticsJsonOptions));
        return report.Passed ? 0 : 1;
    }
}
