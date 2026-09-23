using ScreenshotAssistant.Core.Capture;

namespace ScreenshotAssistant.Core.Tests;

public sealed class CaptureSessionGateTests
{
    [Fact]
    public void TryEnter_RejectsConcurrentSession_ThenAllowsNextSession()
    {
        CaptureSessionGate gate = new();

        Assert.True(gate.TryEnter(out IDisposable? firstLease));
        Assert.NotNull(firstLease);
        Assert.False(gate.TryEnter(out IDisposable? rejectedLease));
        Assert.Null(rejectedLease);

        firstLease.Dispose();

        Assert.True(gate.TryEnter(out IDisposable? nextLease));
        nextLease?.Dispose();
    }

    [Fact]
    public void Lease_DisposeIsIdempotent()
    {
        CaptureSessionGate gate = new();
        Assert.True(gate.TryEnter(out IDisposable? lease));

        lease?.Dispose();
        lease?.Dispose();

        Assert.True(gate.TryEnter(out IDisposable? nextLease));
        nextLease?.Dispose();
    }
}
