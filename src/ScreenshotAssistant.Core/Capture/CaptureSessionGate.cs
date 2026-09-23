namespace ScreenshotAssistant.Core.Capture;

public sealed class CaptureSessionGate
{
    private int _activeSession;

    public bool TryEnter(out IDisposable? lease)
    {
        if (Interlocked.CompareExchange(ref _activeSession, 1, 0) != 0)
        {
            lease = null;
            return false;
        }

        lease = new SessionLease(this);
        return true;
    }

    private sealed class SessionLease(CaptureSessionGate owner) : IDisposable
    {
        private CaptureSessionGate? _owner = owner;

        public void Dispose()
        {
            CaptureSessionGate? currentOwner = Interlocked.Exchange(ref _owner, null);
            if (currentOwner is not null)
            {
                Volatile.Write(ref currentOwner._activeSession, 0);
            }
        }
    }
}
