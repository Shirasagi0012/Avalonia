using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.WinRT.Composition;

// Diagnostic only. Delay relative to the observed compositor-clock wake, without busy spinning.
internal sealed class WinUiPresentationPhaseDelay : IDisposable
{
    private readonly long _delayTicks;
    private readonly IntPtr _timer;

    public WinUiPresentationPhaseDelay()
    {
        var value = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_PHASE_MS") ?? "0";
        var ms = double.Parse(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(ms) || ms < 0 || ms > 4)
            throw new ArgumentOutOfRangeException(nameof(value));
        _delayTicks = (long)(ms * Stopwatch.Frequency / 1000);
        WinUiPresentationNative.Trace("PhaseDelayUs", 0, (long)(ms * 1000));
        if (_delayTicks != 0)
        {
            _timer = CreateWaitableTimerExW(IntPtr.Zero, null, 2, 0x1F0003);
            if (_timer == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
        }
    }

    public void Wait(long clockQpc)
    {
        var deadline = clockQpc + _delayTicks;
        WinUiPresentationNative.Trace("SelectedOpportunity", (ulong)clockQpc, deadline);
        var remaining = deadline - Stopwatch.GetTimestamp();
        if (_timer != IntPtr.Zero && remaining > 0)
        {
            var due = -(long)Math.Ceiling(remaining * 10000000.0 / Stopwatch.Frequency);
            if (!SetWaitableTimerEx(_timer, ref due, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0))
                throw new System.ComponentModel.Win32Exception();
            if (WinUiPresentationNative.WaitForSingleObject(_timer, uint.MaxValue) != 0)
                throw new System.ComponentModel.Win32Exception();
        }
    }

    public void Dispose()
    {
        if (_timer != IntPtr.Zero) WinUiPresentationNative.CloseHandle(_timer);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWaitableTimerExW(IntPtr attributes, string? name, uint flags, uint access);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWaitableTimerEx(IntPtr timer, ref long dueTime, int period,
        IntPtr completion, IntPtr argument, IntPtr reason, uint tolerance);
}
