using System.Collections.Generic;

namespace Avalonia.Media
{
    internal interface ITextShaperFontVariationMetadataProvider
    {
        IReadOnlyList<FontVariationAxis> GetVariationAxes(IReadOnlyList<FontVariationAxis> renderingAxes);

        IReadOnlyList<FontVariationNamedInstance> NamedInstances { get; }
    }
}
