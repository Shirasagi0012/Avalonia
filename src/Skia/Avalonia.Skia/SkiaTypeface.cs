using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SkiaTypeface : IPlatformTypeface, IFontVariationMetadataProvider
    {
        private IReadOnlyList<FontVariationAxis>? _variationAxes;

        public SkiaTypeface(SKTypeface typeface, FontSimulations fontSimulations)
            : this(typeface, fontSimulations, null)
        {
        }

        public SkiaTypeface(
            SKTypeface typeface,
            FontSimulations fontSimulations,
            EffectiveVariationCoordinates? variationCoordinates)
        {
            SKTypeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
            VariationSKTypeface = SkiaVariationTypefaceCache.GetOrCreate(typeface, variationCoordinates);
            FontSimulations = fontSimulations;
            Weight = (FontWeight)typeface.FontWeight;
            Style = typeface.FontStyle.Slant.ToAvalonia();
            Stretch = (FontStretch)typeface.FontWidth;
        }

        public SKTypeface SKTypeface { get; }

        public SKTypeface VariationSKTypeface { get; }

        public FontSimulations FontSimulations { get; }

        public string FamilyName => SKTypeface.FamilyName;

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public IReadOnlyList<FontVariationAxis> VariationAxes => _variationAxes ??= LoadVariationAxes();

        public SKFont CreateSKFont(float size)
        {
            return new(VariationSKTypeface, size, skewX: (FontSimulations & FontSimulations.Oblique) != 0 ? -0.3f : 0.0f)
            {
                LinearMetrics = true,
                Embolden = (FontSimulations & FontSimulations.Bold) != 0
            };
        }

        public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
        {
            table = default;

            if (SKTypeface.TryGetTableData(tag, out var data))
            {
                table = data;

                return true;
            }

            return false;
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
        {
            try
            {
                var asset = SKTypeface.OpenStream();
                var size = asset.Length;
                var buffer = new byte[size];

                asset.Read(buffer, size);

                stream = new MemoryStream(buffer);

                return true;
            }
            catch
            {
                stream = null;

                return false;
            }
        }

        public void Dispose()
        {
            SKTypeface.Dispose();
        }

        private IReadOnlyList<FontVariationAxis> LoadVariationAxes()
        {
            var parameters = SKTypeface.VariationDesignParameters;

            if (parameters.Length == 0)
            {
                return Array.Empty<FontVariationAxis>();
            }

            var axes = new FontVariationAxis[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                axes[i] = new FontVariationAxis(
                    parameter.Tag.ToString(),
                    parameter.Min,
                    parameter.Default,
                    parameter.Max,
                    parameter.IsHidden);
            }

            return axes;
        }

        private static class SkiaVariationTypefaceCache
        {
            private const int Capacity = 32;
            private static readonly object s_lock = new();
            private static readonly ConcurrentDictionary<CacheKey, CacheEntry> s_entries = new();
            private static long s_clock;

            public static SKTypeface GetOrCreate(SKTypeface baseTypeface, EffectiveVariationCoordinates? variationCoordinates)
            {
                if (variationCoordinates is not { HasVariations: true })
                {
                    return baseTypeface;
                }

                var coordinates = CreateSkiaCoordinates(baseTypeface, variationCoordinates);
                if (coordinates.Length == 0)
                {
                    return baseTypeface;
                }

                var key = new CacheKey(baseTypeface.Handle, CreateFingerprint(coordinates));

                if (s_entries.TryGetValue(key, out var entry))
                {
                    lock (s_lock)
                    {
                        entry.LastUsed = ++s_clock;
                    }

                    LogCacheSize();

                    return entry.Typeface;
                }

                LogCloneCoordinates(baseTypeface, coordinates);

                var variationTypeface = baseTypeface.Clone(coordinates) ?? baseTypeface;

                lock (s_lock)
                {
                    if (s_entries.TryGetValue(key, out var existing))
                    {
                        existing.LastUsed = ++s_clock;

                        if (!ReferenceEquals(variationTypeface, baseTypeface) && !ReferenceEquals(variationTypeface, existing.Typeface))
                        {
                            variationTypeface.Dispose();
                        }

                        return existing.Typeface;
                    }

                    if (ReferenceEquals(variationTypeface, baseTypeface))
                    {
                        return baseTypeface;
                    }

                    var cachedTypeface = variationTypeface;
                    if (!s_entries.TryAdd(key, new CacheEntry(cachedTypeface, ++s_clock)))
                    {
                        variationTypeface.Dispose();

                        return s_entries[key].Typeface;
                    }

                    TrimCache();
                    LogCacheSize();

                    return cachedTypeface;
                }
            }

            private static SKFontVariationPositionCoordinate[] CreateSkiaCoordinates(
                SKTypeface baseTypeface,
                EffectiveVariationCoordinates variationCoordinates)
            {
                var parameters = baseTypeface.VariationDesignParameters;
                if (parameters.Length == 0)
                {
                    return Array.Empty<SKFontVariationPositionCoordinate>();
                }

                var coordinates = new List<SKFontVariationPositionCoordinate>(variationCoordinates.Count);

                for (var i = 0; i < variationCoordinates.Count; i++)
                {
                    var coordinate = variationCoordinates[i];

                    for (var j = 0; j < parameters.Length; j++)
                    {
                        var parameter = parameters[j];

                        if (string.Equals(parameter.Tag.ToString(), coordinate.Key, StringComparison.Ordinal))
                        {
                            coordinates.Add(new SKFontVariationPositionCoordinate
                            {
                                Axis = SKFourByteTag.Parse(coordinate.Key),
                                Value = Math.Clamp(coordinate.Value, parameter.Min, parameter.Max)
                            });

                            break;
                        }
                    }
                }

                return coordinates.ToArray();
            }

            private static string CreateFingerprint(SKFontVariationPositionCoordinate[] coordinates)
            {
                var builder = new StringBuilder();

                for (var i = 0; i < coordinates.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(';');
                    }

                    builder.Append(coordinates[i].Axis);
                    builder.Append('=');
                    builder.Append(coordinates[i].Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }

            private static void LogCloneCoordinates(SKTypeface baseTypeface, SKFontVariationPositionCoordinate[] coordinates)
            {
                Logger.TryGet(LogEventLevel.Information, "Skia")?.Log(
                    null,
                    "SKTypeface.Clone variation coordinates for {FamilyName}: {Coordinates}",
                    baseTypeface.FamilyName,
                    CreateFingerprint(coordinates));
            }

            private static void LogCacheSize()
            {
                Logger.TryGet(LogEventLevel.Information, "Skia")?.Log(
                    null,
                    "SKTypeface variation transient cache size: {CacheSize}/{Capacity}",
                    s_entries.Count,
                    Capacity);
            }

            private static void TrimCache()
            {
                if (s_entries.Count <= Capacity)
                {
                    return;
                }

                CacheKey oldestKey = default;
                CacheEntry? oldestEntry = null;

                foreach (var entry in s_entries)
                {
                    if (oldestEntry is null || entry.Value.LastUsed < oldestEntry.LastUsed)
                    {
                        oldestKey = entry.Key;
                        oldestEntry = entry.Value;
                    }
                }

                if (s_entries.TryRemove(oldestKey, out var evicted))
                {
                    evicted.Typeface.Dispose();
                }
            }

            private readonly record struct CacheKey(IntPtr TypefaceHandle, string Coordinates);

            private sealed class CacheEntry
            {
                public CacheEntry(SKTypeface typeface, long lastUsed)
                {
                    Typeface = typeface;
                    LastUsed = lastUsed;
                }

                public SKTypeface Typeface { get; }

                public long LastUsed { get; set; }
            }
        }
    }
}
