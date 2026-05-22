using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using HarfBuzzSharp;

namespace Avalonia.Harfbuzz
{
    internal class HarfBuzzTypeface : ITextShaperTypeface, ITextShaperFontVariationMetadataProvider
    {
        private IReadOnlyList<FontVariationNamedInstance>? _namedInstances;

        public HarfBuzzTypeface(GlyphTypeface glyphTypeface)
        {
            GlyphTypeface = glyphTypeface;

            HBFace = new Face(GetTable) { UnitsPerEm = glyphTypeface.Metrics.DesignEmHeight };

            HBFont = new Font(HBFace);

            HBFont.SetFunctionsOpenType();
        }

        public GlyphTypeface GlyphTypeface { get; }
        public Face HBFace { get; }
        public Font HBFont { get; }

        public IReadOnlyList<KeyValuePair<string, float>> EffectiveVariationCoordinates { get; private set; } =
            Array.Empty<KeyValuePair<string, float>>();

        public IReadOnlyList<FontVariationNamedInstance> NamedInstances => _namedInstances ??= LoadNamedInstances();

        public void SetVariations(IReadOnlyList<KeyValuePair<string, float>> coordinates)
        {
            EffectiveVariationCoordinates = coordinates;

            if (coordinates.Count == 0)
            {
                HBFont.SetVariations(ReadOnlySpan<Variation>.Empty);
                LogVariations(coordinates);
                return;
            }

            var variations = new Variation[coordinates.Count];

            for (var i = 0; i < coordinates.Count; i++)
            {
                var coordinate = coordinates[i];
                variations[i] = new Variation
                {
                    Tag = OpenTypeTag.Parse(coordinate.Key),
                    Value = coordinate.Value
                };
            }

            HBFont.SetVariations(variations);
            LogVariations(coordinates);
        }

        public IReadOnlyList<FontVariationAxis> GetVariationAxes(IReadOnlyList<FontVariationAxis> renderingAxes)
        {
            if (renderingAxes.Count == 0 || !HBFace.HasVariationData)
            {
                return renderingAxes;
            }

            var axes = new FontVariationAxis[renderingAxes.Count];

            for (var i = 0; i < renderingAxes.Count; i++)
            {
                var axis = renderingAxes[i];
                var tag = (uint)OpenTypeTag.Parse(axis.Tag);

                var displayName = HBFace.TryFindVariationAxis((Tag)tag, out var axisInfo) ?
                    GetName(axisInfo.NameId) :
                    null;

                axes[i] = axis with { DisplayName = displayName };
            }

            return axes;
        }

        private Blob? GetTable(Face face, Tag tag)
        {
            if (!GlyphTypeface.PlatformTypeface.TryGetTable((uint)tag, out var table))
            {
                return null;
            }

            // If table is backed by managed array, pin it and avoid copy.
            if (MemoryMarshal.TryGetArray(table, out var seg))
            {
                var handle = GCHandle.Alloc(seg.Array!, GCHandleType.Pinned);
                var basePtr = handle.AddrOfPinnedObject();
                var ptr = IntPtr.Add(basePtr, seg.Offset);

                var release = new ReleaseDelegate(() => handle.Free());

                return new Blob(ptr, seg.Count, MemoryMode.ReadOnly, release);
            }

            // Fallback: allocate native memory and copy
            var nativePtr = Marshal.AllocHGlobal(table.Length);

            unsafe
            {
                table.Span.CopyTo(new Span<byte>((void*)nativePtr, table.Length));
            }

            var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeHGlobal(nativePtr));

            return new Blob(nativePtr, table.Length, MemoryMode.ReadOnly, releaseDelegate);
        }

        public void Dispose()
        {
            HBFont.Dispose();
            HBFace.Dispose();
        }

        private IReadOnlyList<FontVariationNamedInstance> LoadNamedInstances()
        {
            if (!HBFace.HasVariationData || HBFace.NamedInstanceCount == 0)
            {
                return Array.Empty<FontVariationNamedInstance>();
            }

            var axisInfos = HBFace.VariationAxisInfos;
            var instances = new FontVariationNamedInstance[HBFace.NamedInstanceCount];

            for (var instanceIndex = 0; instanceIndex < instances.Length; instanceIndex++)
            {
                var coords = HBFace.GetNamedInstanceDesignCoords(instanceIndex);
                var coordinateCount = Math.Min(axisInfos.Length, coords.Length);
                IReadOnlyList<KeyValuePair<string, float>> coordinates;

                if (coordinateCount == 0)
                {
                    coordinates = Array.Empty<KeyValuePair<string, float>>();
                }
                else
                {
                    var coordinateList = new KeyValuePair<string, float>[coordinateCount];

                    for (var i = 0; i < coordinateCount; i++)
                    {
                        coordinateList[i] = new KeyValuePair<string, float>(
                            new OpenTypeTag(axisInfos[i].Tag).ToString(),
                            coords[i]);
                    }

                    coordinates = coordinateList;
                }

                instances[instanceIndex] = new FontVariationNamedInstance(
                    instanceIndex,
                    GetName(HBFace.GetNamedInstanceSubfamilyNameId(instanceIndex)),
                    GetName(HBFace.GetNamedInstancePostScriptNameId(instanceIndex)),
                    coordinates);
            }

            return instances;
        }

        private string? GetName(OpenTypeNameId nameId)
        {
            if (nameId == OpenTypeNameId.Invalid)
            {
                return null;
            }

            var value = GlyphTypeface.GetOpenTypeName((ushort)nameId);

            return string.IsNullOrEmpty(value) ? null : value;
        }

        private void LogVariations(IReadOnlyList<KeyValuePair<string, float>> coordinates)
        {
            var logger = Logger.TryGet(LogEventLevel.Information, "HarfBuzz");

            if (logger is null)
            {
                return;
            }

            var formattedCoordinates = FormatCoordinates(coordinates);

            logger.Value.Log(
                this,
                "Applied variable font coordinates. HarfBuzzCoordinates={HarfBuzzCoordinates}; SkiaCoordinates={SkiaCoordinates}",
                formattedCoordinates,
                formattedCoordinates);
        }

        private static string FormatCoordinates(IReadOnlyList<KeyValuePair<string, float>> coordinates)
        {
            if (coordinates.Count == 0)
            {
                return "[]";
            }

            var builder = new StringBuilder();
            builder.Append('[');

            for (var i = 0; i < coordinates.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                var coordinate = coordinates[i];
                builder.Append(coordinate.Key);
                builder.Append('=');
                builder.Append(coordinate.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            builder.Append(']');

            return builder.ToString();
        }

    }
}
