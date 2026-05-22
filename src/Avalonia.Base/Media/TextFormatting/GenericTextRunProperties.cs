using System.Globalization;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextRunProperties
    /// </summary>
    public class GenericTextRunProperties : TextRunProperties
    {
        internal const double DefaultFontRenderingEmSize = 12;

        public GenericTextRunProperties(
            Typeface typeface,
            double fontRenderingEmSize = DefaultFontRenderingEmSize,
            TextDecorationCollection? textDecorations = null,
            IBrush? foregroundBrush = null,
            IBrush? backgroundBrush = null,
            BaselineAlignment baselineAlignment = BaselineAlignment.Baseline,
            CultureInfo? cultureInfo = null,
            FontFeatureCollection? fontFeatures = null,
            FontVariationCollection? fontVariations = null,
            FontVariationNamedInstance? fontVariationNamedInstance = null,
            FontOpticalSizing fontOpticalSizing = FontOpticalSizing.Auto)
        {
            Typeface = fontVariationNamedInstance.HasValue && !typeface.NamedInstance.HasValue
                ? new Typeface(typeface.FontFamily, typeface.Style, typeface.Weight, typeface.Stretch,
                    fontVariations ?? typeface.Variations, fontOpticalSizing, fontVariationNamedInstance)
                : typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            TextDecorations = textDecorations;
            ForegroundBrush = foregroundBrush;
            BackgroundBrush = backgroundBrush;
            BaselineAlignment = baselineAlignment;
            CultureInfo = cultureInfo;
            FontFeatures = fontFeatures;
            FontVariations = fontVariations;
            FontVariationNamedInstance = fontVariationNamedInstance ?? Typeface.NamedInstance;
            FontOpticalSizing = fontOpticalSizing;
        }

        /// <inheritdoc />
        public override Typeface Typeface { get; }

        /// <inheritdoc />
        public override double FontRenderingEmSize { get; }

        /// <inheritdoc />
        public override TextDecorationCollection? TextDecorations { get; }

        /// <inheritdoc />
        public override IBrush? ForegroundBrush { get; }

        /// <inheritdoc />
        public override IBrush? BackgroundBrush { get; }

        /// <inheritdoc />
        public override FontFeatureCollection? FontFeatures { get; }

        /// <inheritdoc />
        public override FontVariationCollection? FontVariations { get; }

        /// <inheritdoc />
        public override FontVariationNamedInstance? FontVariationNamedInstance { get; }

        /// <inheritdoc />
        public override FontOpticalSizing FontOpticalSizing { get; }

        /// <inheritdoc />
        public override BaselineAlignment BaselineAlignment { get; }

        /// <inheritdoc />
        public override CultureInfo? CultureInfo { get; }
    }
}
