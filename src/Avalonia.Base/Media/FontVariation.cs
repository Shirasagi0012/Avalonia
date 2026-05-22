using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Avalonia.Media;

/// <summary>
/// Represents a single axis variation for a variable font, consisting of a four-character axis tag
/// and a floating-point design coordinate value.
/// </summary>
/// <remarks>
/// Axis tags are case-sensitive OpenType variation tags. The constructor accepts any tag that is exactly
/// four ASCII characters. <see cref="Parse(string)"/> and <see cref="TryParse(string?, out FontVariation)"/>
/// accept the CSS <c>font-variation-settings</c> item grammar used by Avalonia:
/// a quoted four-character alphanumeric tag followed by a whitespace-separated invariant-culture number,
/// for example <c>"wght" 650</c>, <c>"wdth" 87.5</c>, or <c>"opsz" 14</c>.
/// Unsupported axes are kept in the value but ignored when resolving coordinates for a font face that does
/// not expose a matching axis.
/// </remarks>
[TypeConverter(typeof(FontVariationConverter))]
public readonly struct FontVariation : IEquatable<FontVariation>
{
    private static readonly Regex s_parseRegex = new Regex(
        @"^\s*""(?<Tag>[a-zA-Z0-9]{4})""\s+(?<Value>-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*$",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    /// <summary>
    /// Gets the four-character OpenType variation axis tag.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the floating-point design coordinate value for this axis.
    /// </summary>
    public float Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariation"/> struct.
    /// </summary>
    /// <param name="tag">The four-character axis tag. Must be exactly four ASCII characters.</param>
    /// <param name="value">The design coordinate value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is not exactly four ASCII characters.</exception>
    public FontVariation(string tag, float value)
    {
        ValidateTag(tag);
        Tag = tag;
        Value = value;
    }

    /// <summary>
    /// Parses a CSS <c>font-variation-settings</c> item into a <see cref="FontVariation"/>.
    /// </summary>
    /// <param name="s">The string to parse, e.g. <c>"wght" 650</c>.</param>
    /// <returns>The parsed <see cref="FontVariation"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the string is not a valid variation setting.</exception>
    public static FontVariation Parse(string s)
    {
        if (s is null)
            throw new ArgumentNullException(nameof(s));

        var match = s_parseRegex.Match(s);
        if (!match.Success)
            throw new ArgumentException($"Invalid font variation specification: '{s}'. Expected format: \"TAG\" value (e.g. \"wght\" 650).", nameof(s));

        var tag = match.Groups["Tag"].Value;
        var value = float.Parse(match.Groups["Value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);

        return new FontVariation(tag, value);
    }

    /// <summary>
    /// Attempts to parse a CSS <c>font-variation-settings</c> item.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When successful, contains the parsed <see cref="FontVariation"/>.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? s, out FontVariation result)
    {
        result = default;

        if (s is null)
            return false;

        var match = s_parseRegex.Match(s);
        if (!match.Success)
            return false;

        var tag = match.Groups["Tag"].Value;
        if (!float.TryParse(match.Groups["Value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return false;

        result = new FontVariation(tag, value);
        return true;
    }

    /// <summary>
    /// Returns the CSS representation of this font variation, e.g. <c>"wght" 650</c>.
    /// </summary>
    public override string ToString()
    {
        return $"\"{Tag}\" {Value.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <inheritdoc />
    public bool Equals(FontVariation other)
    {
        return string.Equals(Tag, other.Tag, StringComparison.Ordinal) &&
               Value.Equals(other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FontVariation other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Tag, Value);
    }

    /// <summary>
    /// Compares two <see cref="FontVariation"/> instances for equality.
    /// </summary>
    public static bool operator ==(FontVariation left, FontVariation right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="FontVariation"/> instances for inequality.
    /// </summary>
    public static bool operator !=(FontVariation left, FontVariation right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Validates that the tag is exactly 4 ASCII-printable characters.
    /// </summary>
    private static void ValidateTag(string tag)
    {
        if (tag is null)
            throw new ArgumentNullException(nameof(tag));

        if (tag.Length != 4)
            throw new ArgumentException($"Axis tag must be exactly 4 characters, got {tag.Length}.", nameof(tag));

        for (int i = 0; i < tag.Length; i++)
        {
            if (tag[i] > 127)
                throw new ArgumentException($"Axis tag must contain only ASCII characters. Character '{tag[i]}' at position {i} is not ASCII.", nameof(tag));
        }
    }
}
