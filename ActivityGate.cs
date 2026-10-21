using System.Runtime.InteropServices;

namespace rans0m;

/// <summary>Waits for quiet time followed by a *new* input event. No keystrokes are captured.</summary>
internal static class ActivityGate
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo { public uint cbSize; public uint dwTime; }
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    private static uint LastInputTick()
    {
        var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info)) throw new InvalidOperationException("GetLastInputInfo failed");
        return info.dwTime;
    }

    public static async Task WaitForNextActivityAsync(TimeSpan quietPeriod, CancellationToken token = default)
    {
        uint quietMilliseconds = (uint)quietPeriod.TotalMilliseconds;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            uint last = LastInputTick();
            // TickCount is also a 32-bit wrapping Windows count; uint subtraction handles rollover.
            uint idleMs = unchecked((uint)Environment.TickCount - last);
            if (idleMs >= quietMilliseconds) break;
            await Task.Delay(150, token);
        }

        uint baseline = LastInputTick();
        while (LastInputTick() == baseline)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(100, token);
        }
    }
}
