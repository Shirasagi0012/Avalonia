using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface.
    /// </summary>
    [DebuggerDisplay("Name = {FontFamily.Name}, Weight = {Weight}, Style = {Style}")]
    public readonly struct Typeface : IEquatable<Typeface>
    {
        private readonly FontVariation[]? _variations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        public Typeface(FontFamily fontFamily,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal,
            FontStretch stretch = FontStretch.Normal)
            : this(fontFamily, style, weight, stretch, null, default, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        /// <param name="variations">The font variation settings.</param>
        /// <param name="opticalSizing">The optical sizing policy.</param>
        /// <param name="namedInstance">The named variation instance.</param>
        public Typeface(FontFamily fontFamily,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch,
            FontVariationCollection? variations,
            FontOpticalSizing opticalSizing = default,
            FontVariationNamedInstance? namedInstance = null)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Font weight must be > 0.");
            }
            
            if ((int)stretch < 1)
            {
                throw new ArgumentException("Font stretch must be > 1.");
            }

            FontFamily = fontFamily ?? FontFamily.Default;
            Style = style;
            Weight = weight;
            Stretch = stretch;
            _variations = SnapshotVariations(variations);
            OpticalSizing = opticalSizing;
            NamedInstance = SnapshotNamedInstance(namedInstance);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        public Typeface(string fontFamilyName,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal,
            FontStretch stretch = FontStretch.Normal)
            : this(string.IsNullOrEmpty(fontFamilyName) ? FontFamily.Default : new FontFamily(fontFamilyName),
                  style, weight, stretch)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        /// <param name="variations">The font variation settings.</param>
        /// <param name="opticalSizing">The optical sizing policy.</param>
        /// <param name="namedInstance">The named variation instance.</param>
        public Typeface(string fontFamilyName,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch,
            FontVariationCollection? variations,
            FontOpticalSizing opticalSizing = default,
            FontVariationNamedInstance? namedInstance = null)
            : this(string.IsNullOrEmpty(fontFamilyName) ? FontFamily.Default : new FontFamily(fontFamilyName),
                  style, weight, stretch, variations, opticalSizing, namedInstance)
        {
        }

        public static Typeface Default { get; } = new Typeface(FontFamily.Default);

        /// <summary>
        /// Gets the font family.
        /// </summary>
        public FontFamily FontFamily { get; }

        /// <summary>
        /// Gets the font style.
        /// </summary>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the font weight.
        /// </summary>
        public FontWeight Weight { get; }
        
        /// <summary>
        /// Gets the font stretch.
        /// </summary>
        public FontStretch Stretch { get; }

        /// <summary>
        /// Gets the font variation settings.
        /// </summary>
        public FontVariationCollection? Variations => _variations is { Length: > 0 } variations ? new FontVariationCollection(variations) : null;

        /// <summary>
        /// Gets the optical sizing policy.
        /// </summary>
        public FontOpticalSizing OpticalSizing { get; }

        /// <summary>
        /// Gets the named variation instance.
        /// </summary>
        public FontVariationNamedInstance? NamedInstance { get; }

        internal IReadOnlyList<FontVariation>? FontVariations => _variations;

        /// <summary>
        /// Gets the glyph typeface.
        /// </summary>
        /// <value>
        /// The glyph typeface.
        /// </value>
        public GlyphTypeface GlyphTypeface
        {
            get
            {
                if(FontManager.Current.TryGetGlyphTypeface(this, out var glyphTypeface))
                {
                    return glyphTypeface;
                }

                throw new InvalidOperationException(
                    $"Could not create glyphTypeface. Font family: {FontFamily?.Name} (key: {FontFamily?.Key}). Style: {Style}. Weight: {Weight}. Stretch: {Stretch}");
            }
        }

        public static bool operator !=(Typeface a, Typeface b)
        {
            return !(a == b);
        }

        public static bool operator ==(Typeface a, Typeface b)
        {
            return  a.Equals(b);
        }

        public override bool Equals(object? obj)
        {
            return obj is Typeface typeface && Equals(typeface);
        }

        public bool Equals(Typeface other)
        {
            return FontFamily == other.FontFamily && Style == other.Style && 
                   Weight == other.Weight && Stretch == other.Stretch &&
                   OpticalSizing == other.OpticalSizing &&
                   VariationsEqual(_variations, other._variations) &&
                   NamedInstancesEqual(NamedInstance, other.NamedInstance);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FontFamily != null ? FontFamily.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Style;
                hashCode = (hashCode * 397) ^ (int)Weight;
                hashCode = (hashCode * 397) ^ (int)Stretch;
                hashCode = (hashCode * 397) ^ (int)OpticalSizing;

                if (_variations is { Length: > 0 })
                {
                    for (var i = 0; i < _variations.Length; i++)
                    {
                        hashCode = (hashCode * 397) ^ _variations[i].GetHashCode();
                    }
                }

                if (NamedInstance is { } namedInstance)
                {
                    hashCode = (hashCode * 397) ^ namedInstance.InstanceIndex;
                    hashCode = (hashCode * 397) ^ (namedInstance.DisplayName?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ (namedInstance.PostScriptName?.GetHashCode() ?? 0);

                    for (var i = 0; i < namedInstance.Coordinates.Count; i++)
                    {
                        var coordinate = namedInstance.Coordinates[i];
                        hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(coordinate.Key);
                        hashCode = (hashCode * 397) ^ coordinate.Value.GetHashCode();
                    }
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            var typeName = typeof(Typeface).FullName ?? nameof(Typeface);

            if (!HasVariationData())
            {
                return typeName;
            }

            var builder = new StringBuilder(typeName);
            builder.Append(" Variations=[");

            if (_variations is { Length: > 0 })
            {
                for (var i = 0; i < _variations.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(_variations[i]);
                }
            }

            builder.Append(']');

            if (OpticalSizing != default)
            {
                builder.Append(", OpticalSizing=");
                builder.Append(OpticalSizing);
            }

            if (NamedInstance is { } namedInstance)
            {
                builder.Append(", NamedInstance=");
                builder.Append(namedInstance.InstanceIndex.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Normalizes the typeface by extracting and removing style, weight, and stretch information from the font
        /// family name, and returns a new <see cref="Typeface"/> instance with the updated properties.
        /// </summary>
        /// <remarks>This method analyzes the font family name to identify and extract any style, weight,
        /// or stretch information embedded within it. If such information is found, it is removed from the family name,
        /// and the corresponding properties of the returned <see cref="Typeface"/> are updated accordingly. If no such
        /// information is found, the method returns the current instance without modification.</remarks>
        /// <param name="normalizedFamilyName">When this method returns, contains the normalized font family name with style, weight, and stretch
        /// information removed. This parameter is passed uninitialized.</param>
        /// <returns>A new <see cref="Typeface"/> instance with the updated <see cref="FontStyle"/>, <see cref="FontWeight"/>,
        /// and <see cref="FontStretch"/> properties, or the current instance if no normalization was performed.</returns>
        public Typeface Normalize(out string normalizedFamilyName)
        {
            normalizedFamilyName = FontFamily.FamilyNames.PrimaryFamilyName;

            //Return early if no separator is present.
            if (!normalizedFamilyName.Contains(' '))
            {
                return this;
            }

            var style = Style;
            var weight = Weight;
            var stretch = Stretch;

            StringBuilder? normalizedFamilyNameBuilder = null;
            var totalCharsRemoved = 0;

            var tokenizer = new SpanStringTokenizer(normalizedFamilyName, ' ');

            // Skip initial family name.
            tokenizer.ReadSpan();

            while (tokenizer.TryReadSpan(out var token))
            {
                // Don't try to match numbers.
                if (new SpanStringTokenizer(token).TryReadInt32(out _))
                {
                    continue;
                }

                // Try match with font style, weight or stretch and update accordingly.
                var match = false;
                if (Enum.TryParse<FontStyle>(token, true, out var newStyle))
                {
                    style = newStyle;
                    match = true;
                }
                else if (Enum.TryParse<FontWeight>(token, true, out var newWeight))
                {
                    weight = newWeight;
                    match = true;
                }
                else if (Enum.TryParse<FontStretch>(token, true, out var newStretch))
                {
                    stretch = newStretch;
                    match = true;
                }

                if (match)
                {
                    // Carve out matched word from the normalized name.
                    normalizedFamilyNameBuilder ??= new StringBuilder(normalizedFamilyName);
                    normalizedFamilyNameBuilder.Remove(tokenizer.CurrentTokenIndex - totalCharsRemoved, token.Length);
                    totalCharsRemoved += token.Length;
                }
            }

            // Get rid of any trailing spaces.
            normalizedFamilyName = (normalizedFamilyNameBuilder?.ToString() ?? normalizedFamilyName).TrimEnd();

            //Preserve old font source
            return new Typeface(FontFamily, style, weight, stretch, Variations, OpticalSizing, NamedInstance);
        }

        internal Typeface WithFontFamily(FontFamily fontFamily)
        {
            return new Typeface(fontFamily, Style, Weight, Stretch, Variations, OpticalSizing, NamedInstance);
        }

        private bool HasVariationData()
        {
            return _variations is { Length: > 0 } || OpticalSizing != default || NamedInstance.HasValue;
        }

        private static FontVariation[]? SnapshotVariations(IReadOnlyList<FontVariation>? variations)
        {
            if (variations is null || variations.Count == 0)
            {
                return null;
            }

            var byTag = new Dictionary<string, FontVariation>(variations.Count, StringComparer.Ordinal);

            for (var i = 0; i < variations.Count; i++)
            {
                var variation = variations[i];
                byTag[variation.Tag] = variation;
            }

            if (byTag.Count == 0)
            {
                return null;
            }

            var canonical = new FontVariation[byTag.Count];
            var index = 0;

            foreach (var variation in byTag.Values)
            {
                canonical[index++] = variation;
            }

            Array.Sort(canonical, static (left, right) => string.CompareOrdinal(left.Tag, right.Tag));

            return canonical;
        }

        private static FontVariationNamedInstance? SnapshotNamedInstance(FontVariationNamedInstance? namedInstance)
        {
            if (namedInstance is not { } value)
            {
                return null;
            }

            var byTag = new Dictionary<string, float>(value.Coordinates.Count, StringComparer.Ordinal);

            for (var i = 0; i < value.Coordinates.Count; i++)
            {
                var coordinate = value.Coordinates[i];
                byTag[coordinate.Key] = coordinate.Value;
            }

            var coordinates = new KeyValuePair<string, float>[byTag.Count];
            var index = 0;

            foreach (var coordinate in byTag)
            {
                coordinates[index++] = coordinate;
            }

            Array.Sort(coordinates, static (left, right) => string.CompareOrdinal(left.Key, right.Key));

            return new FontVariationNamedInstance(
                value.InstanceIndex,
                value.DisplayName,
                value.PostScriptName,
                new ReadOnlyCollection<KeyValuePair<string, float>>(coordinates));
        }

        private static bool VariationsEqual(IReadOnlyList<FontVariation>? left, IReadOnlyList<FontVariation>? right)
        {
            var leftCount = left?.Count ?? 0;
            var rightCount = right?.Count ?? 0;

            if (leftCount != rightCount)
            {
                return false;
            }

            for (var i = 0; i < leftCount; i++)
            {
                if (!left![i].Equals(right![i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NamedInstancesEqual(FontVariationNamedInstance? left, FontVariationNamedInstance? right)
        {
            if (!left.HasValue || !right.HasValue)
            {
                return left.HasValue == right.HasValue;
            }

            var leftValue = left.Value;
            var rightValue = right.Value;

            if (leftValue.InstanceIndex != rightValue.InstanceIndex ||
                !string.Equals(leftValue.DisplayName, rightValue.DisplayName, StringComparison.Ordinal) ||
                !string.Equals(leftValue.PostScriptName, rightValue.PostScriptName, StringComparison.Ordinal) ||
                leftValue.Coordinates.Count != rightValue.Coordinates.Count)
            {
                return false;
            }

            for (var i = 0; i < leftValue.Coordinates.Count; i++)
            {
                var leftCoordinate = leftValue.Coordinates[i];
                var rightCoordinate = rightValue.Coordinates[i];

                if (!string.Equals(leftCoordinate.Key, rightCoordinate.Key, StringComparison.Ordinal) ||
                    Math.Abs(leftCoordinate.Value - rightCoordinate.Value) > EffectiveVariationResolver.CoordinateTolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
