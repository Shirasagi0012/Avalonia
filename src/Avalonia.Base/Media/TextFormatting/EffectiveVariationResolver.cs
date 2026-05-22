using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting;

internal static class EffectiveVariationResolver
{
    public const float CoordinateTolerance = 0.0001f;

    private const string WeightTag = "wght";
    private const string WidthTag = "wdth";
    private const string ItalicTag = "ital";
    private const string OpticalSizeTag = "opsz";

    public static EffectiveVariationCoordinates Resolve(VariationResolverInput input)
    {
        return Resolve(new VariationResolverCoordinateInput(
            input.FaceVariationAxes,
            input.Typeface,
            input.FontSize,
            input.FontOpticalSizing,
            input.NamedInstance,
            GetVariationCoordinates(input.FontVariations)));
    }

    public static FontVariationNamedInstance? ResolveNamedInstanceForFace(
        FontVariationNamedInstance? requested,
        IReadOnlyList<FontVariationNamedInstance> faceNamedInstances)
    {
        if (requested is not { } namedInstance || faceNamedInstances.Count == 0)
        {
            return null;
        }

        if (namedInstance.InstanceIndex >= 0 && namedInstance.InstanceIndex < faceNamedInstances.Count)
        {
            var indexedInstance = faceNamedInstances[namedInstance.InstanceIndex];

            if (string.IsNullOrEmpty(namedInstance.PostScriptName) ||
                string.IsNullOrEmpty(indexedInstance.PostScriptName) ||
                string.Equals(namedInstance.PostScriptName, indexedInstance.PostScriptName, StringComparison.Ordinal))
            {
                return indexedInstance;
            }
        }

        if (!string.IsNullOrEmpty(namedInstance.PostScriptName))
        {
            for (var i = 0; i < faceNamedInstances.Count; i++)
            {
                var faceInstance = faceNamedInstances[i];

                if (string.Equals(namedInstance.PostScriptName, faceInstance.PostScriptName, StringComparison.Ordinal))
                {
                    return faceInstance;
                }
            }
        }

        return null;
    }

    public static EffectiveVariationCoordinates Resolve(VariationResolverCoordinateInput input)
    {
        if (input.FaceVariationAxes is null || input.FaceVariationAxes.Count == 0)
        {
            return EffectiveVariationCoordinates.Empty;
        }

        var axes = new Dictionary<string, FontVariationAxis>(input.FaceVariationAxes.Count, StringComparer.Ordinal);

        for (var i = 0; i < input.FaceVariationAxes.Count; i++)
        {
            var axis = input.FaceVariationAxes[i];

            if (!string.IsNullOrEmpty(axis.Tag))
            {
                axes[axis.Tag] = axis;
            }
        }

        if (axes.Count == 0)
        {
            return EffectiveVariationCoordinates.Empty;
        }

        var coordinates = new Dictionary<string, float>(StringComparer.Ordinal);

        if (input.NamedInstance is { Coordinates: { } namedInstanceCoordinates })
        {
            for (var i = 0; i < namedInstanceCoordinates.Count; i++)
            {
                var coordinate = namedInstanceCoordinates[i];
                SetIfSupported(coordinates, axes, coordinate.Key, coordinate.Value);
            }
        }

        SetIfSupported(coordinates, axes, WeightTag, (int)input.Typeface.Weight);
        SetIfSupported(coordinates, axes, WidthTag, (int)input.Typeface.Stretch);

        if (input.Typeface.Style == FontStyle.Italic)
        {
            SetIfSupported(coordinates, axes, ItalicTag, 1f);
        }

        var hasExplicitOpticalSize = HasExplicitVariation(input.FontVariationCoordinates, OpticalSizeTag);

        if (input.FontOpticalSizing == FontOpticalSizing.Auto && !hasExplicitOpticalSize)
        {
            SetIfSupported(coordinates, axes, OpticalSizeTag, (float)input.FontSize);
        }

        if (input.FontVariationCoordinates is not null)
        {
            for (var i = 0; i < input.FontVariationCoordinates.Count; i++)
            {
                var variation = input.FontVariationCoordinates[i];
                SetIfSupported(coordinates, axes, variation.Key, variation.Value);
            }
        }

        if (coordinates.Count == 0)
        {
            return EffectiveVariationCoordinates.Empty;
        }

        var canonical = new List<KeyValuePair<string, float>>(coordinates);
        canonical.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));

        return new EffectiveVariationCoordinates(canonical);
    }

    private static IReadOnlyList<KeyValuePair<string, float>>? GetVariationCoordinates(IReadOnlyList<FontVariation>? variations)
    {
        if (variations is null)
        {
            return null;
        }

        var coordinates = new List<KeyValuePair<string, float>>(variations.Count);

        for (var i = 0; i < variations.Count; i++)
        {
            var variation = variations[i];
            var tag = variation.Tag;
            var value = variation.Value;

            coordinates.Add(new KeyValuePair<string, float>(tag, value));
        }

        return coordinates;
    }

    private static bool HasExplicitVariation(IReadOnlyList<KeyValuePair<string, float>>? variations, string tag)
    {
        if (variations is null)
        {
            return false;
        }

        for (var i = 0; i < variations.Count; i++)
        {
            if (string.Equals(variations[i].Key, tag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void SetIfSupported(
        IDictionary<string, float> coordinates,
        IReadOnlyDictionary<string, FontVariationAxis> axes,
        string tag,
        float value)
    {
        if (!axes.TryGetValue(tag, out var axis))
        {
            return;
        }

        coordinates[tag] = Math.Clamp(value, axis.Min, axis.Max);
    }
}

internal readonly record struct VariationResolverInput(
    IReadOnlyList<FontVariationAxis> FaceVariationAxes,
    Typeface Typeface,
    double FontSize,
    FontOpticalSizing FontOpticalSizing,
    FontVariationNamedInstance? NamedInstance = null,
    IReadOnlyList<FontVariation>? FontVariations = null);

internal readonly record struct VariationResolverCoordinateInput(
    IReadOnlyList<FontVariationAxis> FaceVariationAxes,
    Typeface Typeface,
    double FontSize,
    FontOpticalSizing FontOpticalSizing,
    FontVariationNamedInstance? NamedInstance = null,
    IReadOnlyList<KeyValuePair<string, float>>? FontVariationCoordinates = null);

internal sealed class EffectiveVariationCoordinates : IReadOnlyList<KeyValuePair<string, float>>, IEquatable<EffectiveVariationCoordinates>
{
    public static EffectiveVariationCoordinates Empty { get; } = new(Array.Empty<KeyValuePair<string, float>>());

    private readonly IReadOnlyList<KeyValuePair<string, float>> _coordinates;

    public EffectiveVariationCoordinates(IReadOnlyList<KeyValuePair<string, float>> coordinates)
    {
        _coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
    }

    public bool HasVariations => Count > 0;

    public int Count => _coordinates.Count;

    public KeyValuePair<string, float> this[int index] => _coordinates[index];

    public IEnumerator<KeyValuePair<string, float>> GetEnumerator() => _coordinates.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(EffectiveVariationCoordinates? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || Count != other.Count)
        {
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            var left = this[i];
            var right = other[i];

            if (!string.Equals(left.Key, right.Key, StringComparison.Ordinal) ||
                Math.Abs(left.Value - right.Value) > EffectiveVariationResolver.CoordinateTolerance)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EffectiveVariationCoordinates other && Equals(other);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        for (var i = 0; i < Count; i++)
        {
            var coordinate = this[i];
            hashCode.Add(coordinate.Key, StringComparer.Ordinal);
        }

        return hashCode.ToHashCode();
    }
}
