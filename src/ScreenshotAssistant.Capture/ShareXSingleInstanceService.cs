using ShareX.HelpersLib;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXSingleInstanceService : IDisposable
{
    private const string MutexName = "ScreenshotAssistant.SingleInstance";
    private const string PipeName = "ScreenshotAssistant.CommandPipe";
    private readonly SingleInstanceManager _manager;

    public ShareXSingleInstanceService(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        _manager = new SingleInstanceManager(MutexName, PipeName, arguments);
        _manager.ArgumentsReceived += OnArgumentsReceived;
    }

    public event Action<string[]>? ActivationRequested;

    public bool IsFirstInstance => _manager.IsFirstInstance;

    public void Dispose()
    {
        _manager.ArgumentsReceived -= OnArgumentsReceived;
        _manager.Dispose();
    }

    private void OnArgumentsReceived(string[] arguments) =>
        ActivationRequested?.Invoke(arguments);
}
