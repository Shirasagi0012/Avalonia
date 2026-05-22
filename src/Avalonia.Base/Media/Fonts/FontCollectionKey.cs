using Avalonia.Media.TextFormatting;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a unique key for identifying a font inside a font collection based on style, weight, and stretch attributes.
    /// </summary>
    /// <remarks>Use this key to efficiently look up or group fonts in a collection by their style, weight,
    /// and stretch characteristics.</remarks>
    public readonly record struct FontCollectionKey
    {
        public FontCollectionKey(FontStyle style, FontWeight weight, FontStretch stretch)
            : this(style, weight, stretch, null)
        {
        }

        internal FontCollectionKey(
            FontStyle style,
            FontWeight weight,
            FontStretch stretch,
            EffectiveVariationCoordinates? variationCoordinates)
        {
            Style = style;
            Weight = weight;
            Stretch = stretch;
            VariationCoordinates = variationCoordinates is { HasVariations: true } ? variationCoordinates : null;
        }

        public FontStyle Style { get; init; }

        public FontWeight Weight { get; init; }

        public FontStretch Stretch { get; init; }

        internal EffectiveVariationCoordinates? VariationCoordinates { get; init; }

        public bool HasVariationCoordinates => VariationCoordinates is { HasVariations: true };

        internal FontCollectionKey WithoutVariationCoordinates() => this with { VariationCoordinates = null };
    }
}
