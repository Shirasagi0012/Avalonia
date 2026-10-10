using System;

namespace Avalonia.Skia;

// Temporary opt-in diagnostic bridge. Markers use the existing submission boundaries;
// never flush Skia/GL or wait for the GPU here.
internal static class PresentationGpuProbe
{
    private static readonly bool Enabled = Environment.GetEnvironmentVariable("AVALONIA_PRESENTATION_GPU_PHASES") == "1";
    public static void Mark(int phase)
    {
        if (Enabled && AppContext.GetData("Avalonia.PresentationGpuProbe") is Action<int> mark)
            mark(phase);
    }
}
