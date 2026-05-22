using System.Collections.Generic;

namespace Avalonia.Media
{
    internal interface IFontVariationMetadataProvider
    {
        IReadOnlyList<FontVariationAxis> VariationAxes { get; }
    }
}
