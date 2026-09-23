using System.Windows.Forms;
using ShareX.HelpersLib;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXGlobalHotkeyService : IDisposable
{
    private readonly HotkeyForm _hotkeyForm;
    private HotkeyInfo _captureHotkey;
    private bool _disposed;

    public ShareXGlobalHotkeyService(Keys hotkey)
    {
        _hotkeyForm = new HotkeyForm
        {
            HotkeyRepeatLimit = 1000
        };
        _hotkeyForm.HotkeyPress += OnHotkeyPress;
        _captureHotkey = new HotkeyInfo(hotkey);
        _hotkeyForm.RegisterHotkey(_captureHotkey);
    }

    public event EventHandler? CaptureRequested;

    public bool IsRegistered => _captureHotkey.Status == HotkeyStatus.Registered;

    public Keys Hotkey => _captureHotkey.Hotkey;

    public string DisplayText => new HotkeyInfo(_captureHotkey.Hotkey).ToString();

    public bool TryUpdateHotkey(Keys hotkey)
    {
        if (_captureHotkey.Hotkey == hotkey)
        {
            return IsRegistered;
        }

        HotkeyInfo previous = _captureHotkey;
        if (previous.Status == HotkeyStatus.Registered)
        {
            _hotkeyForm.UnregisterHotkey(previous);
        }

        HotkeyInfo replacement = new(hotkey);
        _hotkeyForm.RegisterHotkey(replacement);
        if (replacement.Status == HotkeyStatus.Registered)
        {
            _captureHotkey = replacement;
            return true;
        }

        _hotkeyForm.RegisterHotkey(previous);
        _captureHotkey = previous;
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hotkeyForm.HotkeyPress -= OnHotkeyPress;
        if (_captureHotkey.Status == HotkeyStatus.Registered)
        {
            _hotkeyForm.UnregisterHotkey(_captureHotkey);
        }

        _hotkeyForm.Dispose();
    }

    private void OnHotkeyPress(ushort id, Keys key, Modifiers modifier)
    {
        if (id == _captureHotkey.ID)
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
