using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.OpenGl.Angle;
using MicroCom.Runtime;
using static Avalonia.Win32.WinRT.Composition.WinUiPresentationNative;

namespace Avalonia.Win32.WinRT.Composition;

// Opt-in, single-window experiment. No changes to the default DrawingSurface path.
internal sealed class WinUiPresentationSurface : EglGlPlatformSurfaceBase, IDisposable, ICompositionEffectsSurface
{
    private readonly WinUiCompositionShared _shared;
    private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
    private WinUiCompositedWindow? _window;
    public WinUiPresentationSurface(WinUiCompositionShared shared, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
    { _shared = shared; _info = info; }
    public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
    {
        if (context is not EglContext { Display: AngleWin32EglDisplay } egl || IntPtr.Size != 8)
            throw new NotSupportedException("Presentation experiment requires x64 ANGLE D3D11.");
        _window ??= new WinUiCompositedWindow(_info, _shared, null);
        return new Target(egl, _shared, _window);
    }
    public bool IsBlurSupported(BlurEffect effect) => effect == BlurEffect.None;
    public void Dispose() { _window?.Dispose(); _window = null; }

    private sealed unsafe class Target : EglPlatformSurfaceRenderTargetBase
    {
        private readonly WinUiCompositionShared _shared;
        private readonly WinUiCompositedWindow _window;
        private readonly AngleWin32EglDisplay _angle;
        private readonly ID3D11Device _device;
        private nint _manager, _presentationSurface, _handle, _statisticsEvent, _lostEvent;
        private ICompositionSurface? _surface;
        private readonly Buffer?[] _buffers = new Buffer?[2];
        private readonly bool _useRetiringFence = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_RETIRING_FENCE") == "1";
        private nint _retiringFence;
        private PixelSize _size;
        private ulong _lastPresent, _acknowledged;
        private ulong _captureOnlyFrame;
        private bool _lost;
        private readonly ReadinessWaiter _readiness;
        private readonly bool _directClear = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_DIRECT_CLEAR") == "1";
        private readonly double _diagnosticWorkMs = double.Parse(Environment.GetEnvironmentVariable("COMP_WORK_MS") ?? "0",
            System.Globalization.CultureInfo.InvariantCulture);
        private nint _immediateContext;
        private WinUiPresentationGpuTiming? _gpuTiming;

        public Target(EglContext context, WinUiCompositionShared shared, WinUiCompositedWindow window) : base(context)
        {
            _shared = shared; _window = window;
            _readiness = new ReadinessWaiter(() => _shared.PresentationReady?.Invoke());
            _angle = (AngleWin32EglDisplay)context.Display;
            _device = MicroComRuntime.CreateProxyFor<ID3D11Device>(_angle.GetDirect3DDevice(), false).CloneReference();
            nint factory = 0;
            try
            {
                Check(CreatePresentationFactory(_device.GetNativeIntPtr(), new Guid("8FB37B58-1D74-4F64-A49C-1F97A80A2EC0"), out factory));
                var supported = ((delegate* unmanaged[Stdcall]<nint, byte>)Slot(factory, 3))(factory);
                var iflip = ((delegate* unmanaged[Stdcall]<nint, byte>)Slot(factory, 4))(factory);
                Trace("Support", supported, iflip);
                QueryInterruptTimePrecise(out var interruptTime);
                Trace("InterruptTime", interruptTime);
                if (supported == 0 || iflip == 0) throw new NotSupportedException("Displayable presentation buffers unsupported.");
                _manager = GetObject(factory, 5);
                if (_useRetiringFence)
                {
                    var iid = new Guid("affde9d1-1df7-4bb7-8a34-0f46251dab80");
                    nint fence = 0;
                    Check(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Slot(_manager, 10))(_manager, &iid, &fence));
                    _retiringFence = fence;
                }
                Trace("RetiringFenceMode", 0, _useRetiringFence ? 1 : 0);
                Check(DCompositionCreateSurfaceHandle(3, 0, out _handle));
                _presentationSurface = AddObject(_manager, 4, _handle);
                ((delegate* unmanaged[Stdcall]<nint, nuint, void>)Slot(_presentationSurface, 3))(_presentationSurface, 1);
                _lostEvent = GetObject(_manager, 12);
                _statisticsEvent = GetObject(_manager, 13);
                for (var kind = 1; kind <= 3; kind++)
                    Check(((delegate* unmanaged[Stdcall]<nint, int, byte, int>)Slot(_manager, 14))(_manager, kind, 1));
                Check(((delegate* unmanaged[Stdcall]<nint, byte, int>)Slot(_manager, 8))(_manager, 1));
                Check(((delegate* unmanaged[Stdcall]<nint, int, int>)Slot(_presentationSurface, 5))(_presentationSurface, 0));
                Check(((delegate* unmanaged[Stdcall]<nint, int, int>)Slot(_presentationSurface, 6))(_presentationSurface, 3));
                var transform = new Transform { M11 = 1, M22 = 1 };
                Check(((delegate* unmanaged[Stdcall]<nint, Transform*, int>)Slot(_presentationSurface, 8))(_presentationSurface, &transform));
                using var interop = shared.Compositor.QueryInterface<ICompositorInterop>();
                _surface = interop.CreateCompositionSurfaceForHandle(_handle);
                using (_window.BeginTransaction())
                {
                    _window.SetSurface(_surface);
                    _window.ApplyEffects(CompositionTransparencyLevel.None, PlatformThemeVariant.Light);
                }
                _shared.PresentationTreeDirty = true;
                if (Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_GPU_TIMING") == "1")
                {
                    _gpuTiming = new WinUiPresentationGpuTiming(_device.GetNativeIntPtr());
                    DiagnosticCpuWorkCompleted = _gpuTiming.MarkCpuEnd;
                    if (Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_GPU_PHASES") == "1")
                        AppContext.SetData("Avalonia.PresentationGpuProbe", (Action<int>)_gpuTiming.MarkPhase);
                }
            }
            catch (Exception e) { Trace("InitializeError", 0, e.HResult); Dispose(); throw; }
            finally { Release(ref factory); }
        }

        private void ReadStatistics()
        {
            while (WaitForSingleObject(_statisticsEvent, 0) == 0)
            {
                var stat = GetObject(_manager, 15);
                try
                {
                    var id = ((delegate* unmanaged[Stdcall]<nint, ulong>)Slot(stat, 3))(stat);
                    var kind = ((delegate* unmanaged[Stdcall]<nint, int>)Slot(stat, 4))(stat);
                    var iid = new Guid(kind == 1 ? "C9ED2A41-79CB-435E-964E-C8553055420C" :
                        kind == 2 ? "AB41D127-C101-4C0A-911D-F9F2E9D08E64" : "8C93BE27-AD94-4DA0-8FD4-2413132D124E");
                    // Status feedback only bounds unprocessed presents; it is NOT display completion.
                    if (kind == 1)
                    {
                        Check(System.Runtime.InteropServices.Marshal.QueryInterface(stat, in iid, out var status));
                        try
                        {
                            var state = ((delegate* unmanaged[Stdcall]<nint, int>)Slot(status, 6))(status);
                            Trace("Status", id, state);
                            _acknowledged = Math.Max(_acknowledged, id);
                        }
                        finally { Release(ref status); }
                    }
                    else
                    {
                        Check(System.Runtime.InteropServices.Marshal.QueryInterface(stat, in iid, out var frame));
                        try
                        {
                            ulong value;
                            if (kind == 2)
                                value = ((delegate* unmanaged[Stdcall]<nint, ulong>)Slot(frame, 6))(frame);
                            else
                            {
                                // C++ instance methods returning a struct use a hidden return buffer
                                // on Windows x64, even for the single-uint64 SystemInterruptTime.
                                value = 0;
                                ((delegate* unmanaged[Stdcall]<nint, ulong*, void>)Slot(frame, 8))(frame, &value);
                            }
                            Trace(kind == 2 ? "CompositionFrame" : "IndependentFlipFrame", id, (long)value);
                        }
                        finally { Release(ref frame); }
                    }
                }
                finally { Release(ref stat); }
            }
        }
        public override PlatformRenderTargetState State
        {
            get
            {
                if (base.IsCorrupted || _lost || WaitForSingleObject(_lostEvent, 0) == 0)
                    return PlatformRenderTargetState.Corrupted;
                ReadStatistics();
                // Bootstrap: do not submit both initially unused buffers before the
                // first present has reached the system queue. In steady state the two
                // buffer-availability events provide backpressure, not statistics.
                if (_buffers[1] == null && _lastPresent > _acknowledged)
                {
                    Trace("WaitStatus", _lastPresent);
                    _readiness.Arm(_statisticsEvent);
                    return PlatformRenderTargetState.NotReadyTryLater;
                }
                foreach (var buffer in _buffers)
                    if (buffer == null || CanRender(buffer)) return PlatformRenderTargetState.Ready;
                Trace("WaitBuffer", _lastPresent);
                // With two buffers and no unprocessed present, either buffer's release
                // permits progress. Pick the older submitted buffer, not the front buffer.
                var oldest = _buffers[0]!.LastPresent < _buffers[1]!.LastPresent ? _buffers[0]! : _buffers[1]!;
                _readiness.Arm(oldest.AvailableEvent, _retiringFence, oldest.LastPresent);
                return PlatformRenderTargetState.NotReadyTryLater;
            }
        }
        private bool CanRender(Buffer buffer)
        {
            if (Available(buffer.PresentationBuffer)) return true;
            if (_retiringFence == 0 || buffer.LastPresent == 0) return false;
            var value = ((delegate* unmanaged[Stdcall]<nint, ulong>)Slot(_retiringFence, 8))(_retiringFence);
            if (value == ulong.MaxValue) throw new InvalidOperationException("Retiring fence device removed.");
            return value >= buffer.LastPresent;
        }
        public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (sceneInfo.TransparencyLevel != CompositionTransparencyLevel.None)
                throw new NotSupportedException("This prototype currently supports opaque windows only.");
            if (!State.IsReady) throw new RenderTargetNotReadyException();
            var contextLock = Context.EnsureCurrent();
            try
            {
                if (_size != sceneInfo.Size)
                {
                    _size = sceneInfo.Size;
                    var rect = new SourceRect { Right = _size.Width, Bottom = _size.Height };
                    Check(((delegate* unmanaged[Stdcall]<nint, SourceRect*, int>)Slot(_presentationSurface, 7))(_presentationSurface, &rect));
                    _window.ResizeIfNeeded(_size);
                    _shared.PresentationTreeDirty = true;
                }
                Buffer? chosen = null;
                for (var i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i] ??= new Buffer(_device, _angle, _manager, _size);
                    // Resizing still requires a fully available buffer: retiring only
                    // authorizes submitting new drawing, not destroying the old resource.
                    if (_buffers[i]!.Size == _size ? CanRender(_buffers[i]!) : Available(_buffers[i]!.PresentationBuffer))
                    {
                        // Resize only a free buffer; the other remains valid for scanout.
                        if (_buffers[i]!.Size != _size)
                        {
                            _buffers[i]!.Dispose();
                            _buffers[i] = null;
                            _buffers[i] = new Buffer(_device, _angle, _manager, _size);
                        }
                        chosen = _buffers[i]; break;
                    }
                }
                if (chosen == null) throw new RenderTargetNotReadyException();
                var buffer = chosen;
                if (_retiringFence != 0)
                    Trace("RetiringFenceValue", buffer.LastPresent,
                        (long)((delegate* unmanaged[Stdcall]<nint, ulong>)Slot(_retiringFence, 8))(_retiringFence));
                Trace("BufferAcquired", buffer.LastPresent, Available(buffer.PresentationBuffer) ? 1 : 2);
                Trace("BeginDraw");
                _gpuTiming?.Begin();
                return BeginDraw(buffer.Egl, _size, sceneInfo.Scaling, () =>
                {
                    try
                    {
                        Present(buffer);
                        if (_directClear) _shared.PresentationDiagnosticDraw = DrawDiagnostic;
                    }
                    catch { _lost = true; throw; }
                    finally { contextLock.Dispose(); }
                }, true);
            }
            catch { contextLock.Dispose(); throw; }
        }
        private void EnsureImmediateContext()
        {
            if (_immediateContext != 0) return;
            nint immediate = 0;
            _device.GetImmediateContext(&immediate);
            _immediateContext = immediate;
        }
        private void Present(Buffer buffer, bool direct = false)
        {
            _gpuTiming?.MarkRenderEnd();
            if (buffer.OffscreenTexture != null)
            {
                EnsureImmediateContext();
                Trace("OffscreenCopyStart");
                ((delegate* unmanaged[Stdcall]<nint, nint, nint, void>)Slot(_immediateContext, 47))(
                    _immediateContext, buffer.Texture.GetNativeIntPtr(), buffer.OffscreenTexture.GetNativeIntPtr());
                Trace("OffscreenCopyEnd");
            }
            // Capture-only fallback: RenderDoc injection faults in the IPresentationManager
            // submission path on this machine. Preserve drawing commands but do not present.
            // Never use these frames as presentation/performance evidence.
            if (Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_RENDERDOC_DRAW_ONLY") == "1")
            {
                _gpuTiming?.End(++_captureOnlyFrame);
                _shared.PresentationCount++;
                Trace("RenderDocDrawOnly", _captureOnlyFrame);
                return;
            }
            var id = ((delegate* unmanaged[Stdcall]<nint, ulong>)Slot(_manager, 5))(_manager);
            _gpuTiming?.End(id);
            if (direct || buffer.OffscreenTexture != null)
            {
                EnsureImmediateContext();
                ((delegate* unmanaged[Stdcall]<nint, void>)Slot(_immediateContext, 111))(_immediateContext);
                Trace("NativeFlushEnd");
            }
            Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(_presentationSurface, 4))(_presentationSurface, buffer.PresentationBuffer));
            Check(((delegate* unmanaged[Stdcall]<nint, ulong, int>)Slot(_manager, 6))(_manager, 0));
            Trace("PresentStart", id);
            Check(((delegate* unmanaged[Stdcall]<nint, int>)Slot(_manager, 9))(_manager));
            _lastPresent = id;
            buffer.LastPresent = id;
            _shared.PresentationCount++;
            Trace("PresentEnd", id);
        }
        private void DrawDiagnostic()
        {
            // Fixed-size benchmark: bootstrap uses the normal target; subsequent frames
            // retain its surface, device, buffer pool, availability gate and Present code.
            // Do not use this mode for application UI or resize/lifecycle validation.
            if (!State.IsReady) return;
            using var contextLock = Context.EnsureCurrent();
            try
            {
                Buffer? chosen = null;
                for (var i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i] ??= new Buffer(_device, _angle, _manager, _size);
                    if (Available(_buffers[i]!.PresentationBuffer)) { chosen = _buffers[i]; break; }
                }
                if (chosen == null) return;
                if (_immediateContext == 0)
                {
                    EnsureImmediateContext();
                    Trace("DirectClearEnabled");
                }
                chosen.EnsureRenderTargetView(_device);
                Trace("BeginDraw");
                var end = System.Diagnostics.Stopwatch.GetTimestamp() +
                    (long)(_diagnosticWorkMs * System.Diagnostics.Stopwatch.Frequency / 1000);
                while (System.Diagnostics.Stopwatch.GetTimestamp() < end) { }
                // Continuous slow colour variation makes actual content updates visible.
                var t = System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
                float* colour = stackalloc float[4] { .05f, .15f + .1f * (float)Math.Sin(t * 2), .4f, 1 };
                Trace("NativeClearStart");
                _gpuTiming?.Begin();
                ((delegate* unmanaged[Stdcall]<nint, nint, float*, void>)Slot(_immediateContext, 50))(_immediateContext, chosen.RenderTargetView, colour);
                Present(chosen, true);
            }
            catch { _lost = true; throw; }
        }
        public override void Dispose()
        {
            if (_directClear) _shared.PresentationDiagnosticDraw = null;
            _readiness.Dispose();
            if (_gpuTiming != null)
            {
                DiagnosticCpuWorkCompleted = null;
                AppContext.SetData("Avalonia.PresentationGpuProbe", null);
            }
            _gpuTiming?.Dispose();
            using (Context.EnsureCurrent())
                foreach (var buffer in _buffers) buffer?.Dispose();
            _surface?.Dispose(); _surface = null;
            Release(ref _presentationSurface); Release(ref _manager);
            Release(ref _retiringFence);
            if (_handle != 0) { CloseHandle(_handle); _handle = 0; }
            if (_statisticsEvent != 0) { CloseHandle(_statisticsEvent); _statisticsEvent = 0; }
            if (_lostEvent != 0) { CloseHandle(_lostEvent); _lostEvent = 0; }
            _device.Dispose();
            Release(ref _immediateContext);
        }
        private sealed class Buffer : IDisposable
        {
            public nint PresentationBuffer;
            public nint AvailableEvent;
            public ulong LastPresent;
            public readonly EglSurface Egl;
            public readonly PixelSize Size;
            private readonly ID3D11Texture2D _texture;
            public ID3D11Texture2D Texture => _texture;
            public readonly ID3D11Texture2D? OffscreenTexture;
            public nint RenderTargetView;
            public void EnsureRenderTargetView(ID3D11Device device)
            {
                if (RenderTargetView != 0) return;
                nint view = 0;
                var native = device.GetNativeIntPtr();
                Check(((delegate* unmanaged[Stdcall]<nint, nint, nint, nint*, int>)Slot(native, 9))(native, (OffscreenTexture ?? _texture).GetNativeIntPtr(), 0, &view));
                RenderTargetView = view;
            }
            public Buffer(ID3D11Device device, AngleWin32EglDisplay angle, nint manager, PixelSize size)
            {
                Size = size;
                var desc = new D3D11_TEXTURE2D_DESC
                {
                    Width = (uint)size.Width, Height = (uint)size.Height, MipLevels = 1, ArraySize = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
                    Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                    BindFlags = D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET,
                    MiscFlags = (D3D11_RESOURCE_MISC_FLAG)0x100802
                };
                _texture = device.CreateTexture2D(&desc, IntPtr.Zero);
                try
                {
                    PresentationBuffer = AddObject(manager, 3, _texture.GetNativeIntPtr());
                    AvailableEvent = GetObject(PresentationBuffer, 3);
                    if (Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_OFFSCREEN") == "1")
                    {
                        desc.MiscFlags = 0;
                        OffscreenTexture = device.CreateTexture2D(&desc, IntPtr.Zero);
                        Trace("OffscreenCreated", (ulong)size.Width, size.Height);
                    }
                    Egl = angle.WrapDirect3D11Texture((OffscreenTexture ?? _texture).GetNativeIntPtr(), 0, 0, size.Width, size.Height);
                    Trace("BufferCreated", (ulong)size.Width, size.Height);
                }
                catch { if (AvailableEvent != 0) CloseHandle(AvailableEvent); Release(ref PresentationBuffer); OffscreenTexture?.Dispose(); _texture.Dispose(); throw; }
            }
            public void Dispose() { Release(ref RenderTargetView); Egl.Dispose(); OffscreenTexture?.Dispose(); CloseHandle(AvailableEvent); Release(ref PresentationBuffer); _texture.Dispose(); }
        }
    }
}
