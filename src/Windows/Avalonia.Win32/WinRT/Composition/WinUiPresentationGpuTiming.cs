using System;
using System.Runtime.InteropServices;
using static Avalonia.Win32.WinRT.Composition.WinUiPresentationNative;

namespace Avalonia.Win32.WinRT.Composition;

// Diagnostic GPU timeline interval, not GPU busy time. No additional Flush or blocking GetData.
internal sealed unsafe class WinUiPresentationGpuTiming : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DisjointData { public ulong Frequency; public int Disjoint; }
    [StructLayout(LayoutKind.Sequential)]
    private struct PipelineData { public fixed ulong Values[11]; }
    private sealed class Sample
    {
        public nint Begin, CpuEnd, RenderEnd, End, Disjoint;
        public readonly nint[] Phases = new nint[3];
        public int PhaseMask;
        public readonly nint[] Pipeline = new nint[4];
        public int PipelineStage, PipelineMask;
        public bool CpuMarked;
        public bool Pending;
        public ulong Id;
        public long Sequence;
    }
    private nint _context;
    private readonly nint _device;
    private nint _renderDocApi;
    private bool _renderDocCapturing;
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern nint GetProcAddress(nint module, string name);
    private readonly Sample[] _samples = new Sample[8];
    private Sample? _active;
    private long _sequence;
    private readonly bool _phases = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_GPU_PHASES") == "1";
    private readonly bool _pipeline = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_PIPELINE_STATS") == "1"
        && Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_GPU_PHASES") == "1";
    public WinUiPresentationGpuTiming(nint device)
    {
        _device = device;
        nint context = 0;
        ((delegate* unmanaged[Stdcall]<nint, nint*, void>)Slot(device, 40))(device, &context);
        _context = context;
        try
        {
            for (var i = 0; i < _samples.Length; i++)
            {
                var s = _samples[i] = new Sample();
                s.Begin = CreateQuery(device, 2);
                s.CpuEnd = CreateQuery(device, 2);
                s.RenderEnd = CreateQuery(device, 2);
                s.End = CreateQuery(device, 2);
                s.Disjoint = CreateQuery(device, 3);
                if (_phases)
                    for (var j = 0; j < s.Phases.Length; j++) s.Phases[j] = CreateQuery(device, 2);
                if (_pipeline)
                    for (var j = 0; j < s.Pipeline.Length; j++) s.Pipeline[j] = CreateQuery(device, 4);
            }
        }
        catch { Dispose(); throw; }
    }
    private static nint CreateQuery(nint device, int kind)
    {
        int* desc = stackalloc int[2] { kind, 0 };
        nint result = 0;
        Check(((delegate* unmanaged[Stdcall]<nint, int*, nint*, int>)Slot(device, 24))(device, desc, &result));
        return result;
    }
    private void Command(int slot, nint query) =>
        ((delegate* unmanaged[Stdcall]<nint, nint, void>)Slot(_context, slot))(_context, query);
    private bool Get(nint query, void* data, uint size)
    {
        var hr = ((delegate* unmanaged[Stdcall]<nint, nint, void*, uint, uint, int>)Slot(_context, 29))(_context, query, data, size, 1);
        Check(hr);
        return hr == 0;
    }
    public void Begin()
    {
        _sequence++;
        PipelineData* pipeline = stackalloc PipelineData[4];
        foreach (var s in _samples)
        {
            if (!s.Pending || _sequence - s.Sequence < 4) continue;
            DisjointData disjoint = default;
            ulong start = 0, cpuEnd = 0, renderEnd = 0, end = 0;
            ulong layerEnd = 0, targetClearEnd = 0, blitEnd = 0;
            if (!Get(s.Disjoint, &disjoint, (uint)sizeof(DisjointData)) ||
                !Get(s.Begin, &start, sizeof(ulong)) || !Get(s.RenderEnd, &renderEnd, sizeof(ulong)) ||
                !Get(s.End, &end, sizeof(ulong)) || !Get(s.CpuEnd, &cpuEnd, sizeof(ulong))) continue;
            if (s.PhaseMask == 7 && (!Get(s.Phases[0], &layerEnd, sizeof(ulong)) ||
                !Get(s.Phases[1], &targetClearEnd, sizeof(ulong)) || !Get(s.Phases[2], &blitEnd, sizeof(ulong)))) continue;
            if (_pipeline)
            {
                var ready = true;
                for (var j = 0; j < 4; j++)
                    if ((s.PipelineMask & (1 << j)) != 0 && !Get(s.Pipeline[j], pipeline + j, (uint)sizeof(PipelineData))) ready = false;
                if (!ready) continue;
                Trace("GpuPipelineMask", s.Id, s.PipelineMask);
                for (var j = 0; j < 4; j++)
                    if ((s.PipelineMask & (1 << j)) != 0)
                    {
                        Trace("GpuVertices" + j, s.Id, (long)pipeline[j].Values[0]);
                        Trace("GpuPrimitives" + j, s.Id, (long)pipeline[j].Values[1]);
                        Trace("GpuVS" + j, s.Id, (long)pipeline[j].Values[2]);
                        Trace("GpuPS" + j, s.Id, (long)pipeline[j].Values[7]);
                        Trace("GpuCS" + j, s.Id, (long)pipeline[j].Values[10]);
                    }
            }
            if (disjoint.Disjoint == 0 && disjoint.Frequency != 0 && end >= renderEnd && renderEnd >= start)
            {
                Trace("GpuIntervalNs", s.Id, (long)((end - start) * (1e9 / disjoint.Frequency)));
                Trace("GpuRenderNs", s.Id, (long)((renderEnd - start) * (1e9 / disjoint.Frequency)));
                Trace("GpuCopyTailNs", s.Id, (long)((end - renderEnd) * (1e9 / disjoint.Frequency)));
                if (s.PhaseMask == 7 && start <= layerEnd && layerEnd <= targetClearEnd && targetClearEnd <= blitEnd && blitEnd <= renderEnd)
                {
                    Trace("GpuLayerNs", s.Id, (long)((layerEnd - start) * (1e9 / disjoint.Frequency)));
                    Trace("GpuTargetClearNs", s.Id, (long)((targetClearEnd - layerEnd) * (1e9 / disjoint.Frequency)));
                    Trace("GpuBlitNs", s.Id, (long)((blitEnd - targetClearEnd) * (1e9 / disjoint.Frequency)));
                    Trace("GpuAfterBlitNs", s.Id, (long)((renderEnd - blitEnd) * (1e9 / disjoint.Frequency)));
                }
                else if (_phases) Trace("GpuPhaseInvalid", s.Id, s.PhaseMask);
                if (s.CpuMarked && cpuEnd >= start && cpuEnd <= renderEnd)
                {
                    Trace("GpuBeforeCpuMarkerNs", s.Id, (long)((cpuEnd - start) * (1e9 / disjoint.Frequency)));
                    Trace("GpuAfterCpuMarkerNs", s.Id, (long)((renderEnd - cpuEnd) * (1e9 / disjoint.Frequency)));
                }
            }
            else Trace("GpuDisjoint", s.Id);
            s.Pending = false;
        }
        _active = null;
        foreach (var s in _samples)
            if (!s.Pending) { _active = s; break; }
        if (_active == null) { Trace("GpuQuerySkipped"); return; }
        _active.Sequence = _sequence;
        _active.CpuMarked = false;
        _active.PhaseMask = 0;
        _active.PipelineStage = 0;
        _active.PipelineMask = 0;
        if (_sequence == 240 && Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_RENDERDOC") == "1")
        {
            var module = GetModuleHandle("renderdoc.dll");
            var getApi = module == 0 ? 0 : GetProcAddress(module, "RENDERDOC_GetAPI");
            nint api = 0;
            if (getApi != 0 && ((delegate* unmanaged[Cdecl]<int, nint*, int>)getApi)(10400, &api) == 1)
            {
                _renderDocApi = api;
                ((delegate* unmanaged[Cdecl]<uint, uint, void>)((nint*)api)[8])(0, 0);
                ((delegate* unmanaged[Cdecl]<nint, nint, void>)((nint*)api)[19])(_device, 0);
                _renderDocCapturing = true;
                Trace("RenderDocStarted", (ulong)_sequence);
            }
            else Trace("RenderDocUnavailable");
        }
        Command(27, _active.Disjoint);
        Command(28, _active.Begin);
        if (_pipeline) Command(27, _active.Pipeline[0]);
    }
    public void MarkRenderEnd()
    {
        if (_active != null)
        {
            if (!_active.CpuMarked) Command(28, _active.CpuEnd);
            Command(28, _active.RenderEnd);
        }
    }
    public void MarkPhase(int phase)
    {
        if (!_phases || _active == null || phase < 0 || phase >= 3) return;
        if ((_active.PhaseMask & (1 << phase)) != 0) { Trace("GpuPhaseRepeated", (ulong)_sequence, phase); return; }
        if (_pipeline && _active.PipelineStage == phase)
        {
            Command(28, _active.Pipeline[phase]);
            _active.PipelineMask |= 1 << phase;
            _active.PipelineStage++;
            Command(27, _active.Pipeline[phase + 1]);
        }
        Command(28, _active.Phases[phase]);
        _active.PhaseMask |= 1 << phase;
        Trace("GpuPhaseCpu", (ulong)_sequence, phase);
    }
    public void MarkCpuEnd()
    {
        if (_active == null || _active.CpuMarked) return;
        Command(28, _active.CpuEnd);
        _active.CpuMarked = true;
        Trace("CpuWorkCompleteMarker");
    }
    public void End(ulong id)
    {
        if (_active == null) return;
        if (_pipeline)
        {
            Command(28, _active.Pipeline[_active.PipelineStage]);
            _active.PipelineMask |= 1 << _active.PipelineStage;
        }
        Command(28, _active.End);
        Command(28, _active.Disjoint);
        _active.Id = id;
        if (_phases) Trace("GpuPhaseFrame", id, _active.Sequence);
        _active.Pending = true;
        _active = null;
        if (_renderDocCapturing)
        {
            var ok = ((delegate* unmanaged[Cdecl]<nint, nint, uint>)((nint*)_renderDocApi)[21])(_device, 0);
            Trace("RenderDocEnded", id, ok);
            _renderDocCapturing = false;
        }
    }
    public void Dispose()
    {
        foreach (var s in _samples)
            if (s != null)
            {
                Release(ref s.Begin); Release(ref s.CpuEnd); Release(ref s.RenderEnd); Release(ref s.End); Release(ref s.Disjoint);
                for (var j = 0; j < s.Phases.Length; j++) Release(ref s.Phases[j]);
                for (var j = 0; j < s.Pipeline.Length; j++) Release(ref s.Pipeline[j]);
            }
        Release(ref _context);
    }
}
