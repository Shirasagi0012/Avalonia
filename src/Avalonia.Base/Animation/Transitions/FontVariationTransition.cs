using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="FontVariationCollection"/> types.
    /// </summary>
    public class FontVariationTransition : InterpolatingTransitionBase<FontVariationCollection>
    {
        /// <inheritdoc/>
        protected override FontVariationCollection Interpolate(
            double progress,
            FontVariationCollection from,
            FontVariationCollection to)
        {
            return FontVariationCollectionAnimator.InterpolateCore(progress, from, to);
        }
    }
}
