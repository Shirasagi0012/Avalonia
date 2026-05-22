using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Avalonia.Collections;

namespace Avalonia.Media;

/// <summary>
/// Represents a collection of <see cref="FontVariation"/> settings for variable fonts,
/// parseable from CSS <c>font-variation-settings</c> syntax.
/// </summary>
/// <remarks>
/// The accepted grammar is a comma-separated list of <see cref="FontVariation"/> items, such as
/// <c>"wght" 650, "wdth" 87.5, "opsz" 14</c>. Empty comma-separated entries are ignored.
/// Duplicate axis tags are allowed in the collection. When a typeface snapshots the collection or when
/// effective rendering coordinates are resolved, later declarations for the same tag replace earlier ones.
/// During rendering, coordinates are applied only to axes exposed by the selected font face; unsupported axes
/// are ignored. Explicit collection entries are resolved after the named instance, <see cref="FontWeight"/>,
/// <see cref="FontStretch"/>, italic style, and automatic optical sizing, so explicit entries take precedence.
/// </remarks>
[TypeConverter(typeof(FontVariationCollectionConverter))]
public class FontVariationCollection : AvaloniaList<FontVariation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariationCollection"/>.
    /// </summary>
    public FontVariationCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariationCollection"/> that is empty
    /// and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The number of font variations that the new collection can initially store.</param>
    public FontVariationCollection(int capacity) : base(capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariationCollection"/> that contains
    /// font variations copied from the specified collection.
    /// </summary>
    /// <param name="fontVariations">The collection whose font variations are copied to the new collection.</param>
    public FontVariationCollection(IEnumerable<FontVariation> fontVariations) : base(fontVariations)
    {
    }

    /// <summary>
    /// Parses a CSS <c>font-variation-settings</c> string into a <see cref="FontVariationCollection"/>.
    /// </summary>
    /// <param name="s">The string to parse, e.g. <c>"wght" 650, "wdth" 87.5</c>.</param>
    /// <returns>The parsed <see cref="FontVariationCollection"/>.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid font variation settings specification.</exception>
    public static FontVariationCollection Parse(string s)
    {
        if (s is null)
            throw new ArgumentNullException(nameof(s));

        var variations = new List<FontVariation>();

        foreach (var part in SplitByCommaOutsideQuotes(s))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
                continue;

            var variation = FontVariation.Parse(trimmed);
            variations.Add(variation);
        }

        return new FontVariationCollection(variations);
    }

    /// <summary>
    /// Attempts to parse a CSS <c>font-variation-settings</c> string.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When successful, contains the parsed <see cref="FontVariationCollection"/>.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? s, out FontVariationCollection? result)
    {
        result = null;

        if (s is null)
            return false;

        try
        {
            result = Parse(s);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the CSS representation of this font variation collection,
    /// e.g. <c>"wght" 650, "wdth" 87.5</c>.
    /// </summary>
    public override string ToString()
    {
        if (Count == 0)
            return string.Empty;

        var sb = new StringBuilder(Count * 32);

        for (int i = 0; i < Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            sb.Append(this[i].ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Splits a CSS font-variation-settings string by commas that are not inside quoted strings.
    /// Comma-separated entries follow the pattern: "tag" value, "tag" value, ...
    /// </summary>
    private static IEnumerable<string> SplitByCommaOutsideQuotes(string s)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '"')
            {
                depth++;
            }
            else if (s[i] == ',' && depth % 2 == 0)
            {
                parts.Add(s.Substring(start, i - start));
                start = i + 1;
            }
        }

        parts.Add(s.Substring(start));

        return parts;
    }
}
