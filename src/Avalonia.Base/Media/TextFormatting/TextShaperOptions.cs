using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Options to customize text shaping.
    /// </summary>
    public readonly record struct TextShaperOptions
    {
        public TextShaperOptions(
            GlyphTypeface typeface, 
            double fontRenderingEmSize = GenericTextRunProperties.DefaultFontRenderingEmSize,
            sbyte bidiLevel = 0, 
            CultureInfo? culture = null, 
            double incrementalTabWidth = 0,
            double letterSpacing = 0,
            IReadOnlyList<FontFeature>? fontFeatures = null,
            FontVariationCollection? fontVariations = null,
            FontVariationNamedInstance? fontVariationNamedInstance = null,
            FontOpticalSizing fontOpticalSizing = FontOpticalSizing.Auto)
            : this(default, typeface, fontRenderingEmSize, bidiLevel, culture, incrementalTabWidth, letterSpacing,
                fontFeatures, fontVariations, fontVariationNamedInstance, fontOpticalSizing)
        {
        }

        public TextShaperOptions(
            Typeface sourceTypeface,
            GlyphTypeface typeface,
            double fontRenderingEmSize = GenericTextRunProperties.DefaultFontRenderingEmSize,
            sbyte bidiLevel = 0,
            CultureInfo? culture = null,
            double incrementalTabWidth = 0,
            double letterSpacing = 0,
            IReadOnlyList<FontFeature>? fontFeatures = null,
            FontVariationCollection? fontVariations = null,
            FontVariationNamedInstance? fontVariationNamedInstance = null,
            FontOpticalSizing fontOpticalSizing = FontOpticalSizing.Auto)
        {
            SourceTypeface = sourceTypeface;
            GlyphTypeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            Culture = culture;
            IncrementalTabWidth = incrementalTabWidth;
            LetterSpacing = letterSpacing;
            FontFeatures = fontFeatures;
            FontVariations = fontVariations;
            FontVariationNamedInstance = fontVariationNamedInstance ?? sourceTypeface.NamedInstance;
            FontOpticalSizing = fontOpticalSizing;
        }

        /// <summary>
        /// Get the typeface.
        /// </summary>
        public GlyphTypeface GlyphTypeface { get; }

        /// <summary>
        /// Gets the requested typeface that produced <see cref="GlyphTypeface"/>.
        /// </summary>
        internal Typeface SourceTypeface { get; }

        /// <summary>
        /// Get the font rendering em size.
        /// </summary>
        public double FontRenderingEmSize { get; }

        /// <summary>
        /// Get the bidi level of the text.
        /// </summary>
        public sbyte BidiLevel { get; }

        /// <summary>
        /// Get the culture.
        /// </summary>
        public CultureInfo? Culture { get; }

        /// <summary>
        /// Get the incremental tab width.
        /// </summary>
        public double IncrementalTabWidth { get; }

        /// <summary>
        /// Get the letter spacing.
        /// </summary>
        public double LetterSpacing { get; }

        /// <summary>
        /// Get features.
        /// </summary>
        public IReadOnlyList<FontFeature>? FontFeatures { get; }

        /// <summary>
        /// Get variations.
        /// </summary>
        public FontVariationCollection? FontVariations { get; }

        /// <summary>
        /// Get the selected named variation instance.
        /// </summary>
        public FontVariationNamedInstance? FontVariationNamedInstance { get; }

        /// <summary>
        /// Get optical sizing behavior.
        /// </summary>
        public FontOpticalSizing FontOpticalSizing { get; }
    }
}
