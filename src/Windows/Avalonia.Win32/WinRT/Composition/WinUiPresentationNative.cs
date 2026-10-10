using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Win32.WinRT.Composition;

// Experimental x64 ABI bindings, verified against Windows SDK 10.0.26100 presentation.h.
internal static unsafe class WinUiPresentationNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Transform { public float M11, M12, M21, M22, M31, M32; }
    [StructLayout(LayoutKind.Sequential)]
    public struct SourceRect { public int Left, Top, Right, Bottom; }
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("AVALONIA_WUC_PRESENTATION") == "1";
    // Demo-only marker, invoked after the artificial CPU delay and before its drawing commands.
    public static Action? DiagnosticCpuWorkCompleted;
    public static void MarkCpuWorkComplete() => DiagnosticCpuWorkCompleted?.Invoke();
    private static readonly ConcurrentQueue<string> Events = new();
    private static int _eventCount;
    private static readonly string? TracePath = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_TRACE");
    static WinUiPresentationNative()
    {
        if (TracePath != null)
            AppDomain.CurrentDomain.ProcessExit += (_, _) => File.WriteAllLines(TracePath, Events);
    }
    public static void Trace(string name, ulong id = 0, long value = 0)
    {
        if (TracePath != null && Interlocked.Increment(ref _eventCount) <= 200000)
            Events.Enqueue($"{Stopwatch.GetTimestamp()},{name},{id},{value}");
    }
    public static nint Slot(nint obj, int slot) => (*(nint**)obj)[slot];
    public static void Check(int hr) => Marshal.ThrowExceptionForHR(hr);
    public static void Release(ref nint ptr)
    {
        if (ptr != 0) { Marshal.Release(ptr); ptr = 0; }
    }
    public static nint GetObject(nint obj, int slot)
    {
        nint result = 0;
        Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Slot(obj, slot))(obj, &result));
        return result;
    }
    public static nint AddObject(nint obj, int slot, nint argument)
    {
        nint result = 0;
        Check(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)Slot(obj, slot))(obj, argument, &result));
        return result;
    }
    public static bool Available(nint buffer)
    {
        byte available = 0;
        Check(((delegate* unmanaged[Stdcall]<nint, byte*, int>)Slot(buffer, 4))(buffer, &available));
        return available != 0;
    }
    [DllImport("dcomp.dll")]
    public static extern int CreatePresentationFactory(nint device, in Guid iid, out nint factory);
    [DllImport("dcomp.dll")]
    public static extern int DCompositionCreateSurfaceHandle(uint access, nint attributes, out nint handle);
    [DllImport("dcomp.dll")]
    public static extern uint DCompositionWaitForCompositorClock(uint count, nint handles, uint timeout);
    [DllImport("kernel32.dll")]
    public static extern uint WaitForSingleObject(nint handle, uint timeout);
    [DllImport("kernel32.dll")]
    private static extern uint WaitForMultipleObjects(uint count, nint* handles, bool waitAll, uint timeout);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateEventW(nint attributes, bool manualReset, bool initialState, string? name);
    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(nint handle);
    [DllImport("api-ms-win-core-realtime-l1-1-1.dll")]
    public static extern void QueryInterruptTimePrecise(out ulong time);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(nint sourceProcess, nint source, nint targetProcess,
        out nint target, uint access, bool inherit, uint options);

    // Only waits when a clock opportunity was blocked. Duplicating the handle keeps
    // resizing/disposal from closing an event underneath a native wait.
    public sealed class ReadinessWaiter : IDisposable
    {
        private readonly AutoResetEvent _wake = new(false);
        private readonly Thread _thread;
        private readonly object _gate = new();
        private readonly Action _ready;
        private nint _handle;
        private nint _fenceEvent;
        private ulong _fenceValue;
        private bool _armed;
        private volatile bool _stop;
        public ReadinessWaiter(Action ready)
        {
            _ready = ready;
            _thread = new Thread(Run) { IsBackground = true, Name = "PresentationReadiness" };
            _thread.Start();
        }
        public void Arm(nint handle, nint fence = 0, ulong fenceValue = 0)
        {
            lock (_gate)
            {
                if (_stop || _armed) return;
                if (!DuplicateHandle(-1, handle, -1, out _handle, 0, false, 2))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                if (fence != 0)
                {
                    // A fresh event per registration avoids resetting an event before
                    // the waiter has consumed its previous signal. Also wait for fully
                    // available: skipped presents need not signal the retiring fence.
                    _fenceEvent = CreateEventW(0, true, false, null);
                    if (_fenceEvent == 0) { CloseHandle(_handle); _handle = 0; throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); }
                    var hr = ((delegate* unmanaged[Stdcall]<nint, ulong, nint, int>)Slot(fence, 9))(fence, fenceValue, _fenceEvent);
                    if (hr < 0) { CloseHandle(_handle); CloseHandle(_fenceEvent); _handle = _fenceEvent = 0; Check(hr); }
                    _fenceValue = fenceValue;
                }
                _armed = true;
                _wake.Set();
            }
        }
        private void Run()
        {
            var handles = stackalloc nint[2];
            while (true)
            {
                _wake.WaitOne();
                nint handle, fenceEvent;
                ulong fenceValue;
                lock (_gate) { handle = _handle; fenceEvent = _fenceEvent; fenceValue = _fenceValue; _handle = _fenceEvent = 0; }
                if (handle != 0)
                {
                    handles[0] = handle;
                    handles[1] = fenceEvent;
                    var signaled = false;
                    while (!_stop)
                    {
                        var result = fenceEvent == 0 ? WaitForSingleObject(handle, 100) : WaitForMultipleObjects(2, handles, false, 100);
                        if (result == 0 || (result == 1 && fenceEvent != 0))
                        {
                            if (fenceEvent != 0) Trace(result == 1 ? "ReadyRetiringFence" : "ReadyAvailable", fenceValue);
                            if (fenceEvent != 0) Trace("RetiringSignalObserved", fenceValue, WaitForSingleObject(fenceEvent, 0) == 0 ? 1 : 0);
                            signaled = true; break;
                        }
                        if (result != 258) break;
                    }
                    CloseHandle(handle);
                    if (fenceEvent != 0) CloseHandle(fenceEvent);
                    lock (_gate) _armed = false;
                    if (signaled && !_stop) { Trace("ReadySignal"); _ready(); }
                }
                if (_stop) return;
            }
        }
        public void Dispose()
        {
            lock (_gate) { _stop = true; _wake.Set(); }
            _thread.Join();
            _wake.Dispose();
        }
    }
}
