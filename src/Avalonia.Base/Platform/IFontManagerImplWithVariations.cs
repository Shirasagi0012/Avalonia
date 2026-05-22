using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Platform
{
    internal interface IFontManagerImplWithVariations
    {
        bool TryCreateGlyphTypeface(
            string familyName,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch,
            EffectiveVariationCoordinates variationCoordinates,
            [NotNullWhen(returnValue: true)] out IPlatformTypeface? platformTypeface);

        bool TryCreateGlyphTypeface(
            Stream stream,
            FontSimulations fontSimulations,
            EffectiveVariationCoordinates variationCoordinates,
            [NotNullWhen(returnValue: true)] out IPlatformTypeface? platformTypeface);
    }
}
