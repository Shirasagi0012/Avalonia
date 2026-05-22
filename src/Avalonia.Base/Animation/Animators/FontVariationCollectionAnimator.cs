using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="FontVariationCollection"/> properties.
    /// </summary>
    internal class FontVariationCollectionAnimator : InterpolatingAnimator<FontVariationCollection>
    {
        /// <inheritdoc/>
        public override FontVariationCollection Interpolate(
            double progress,
            FontVariationCollection oldValue,
            FontVariationCollection newValue)
        {
            return InterpolateCore(progress, oldValue, newValue);
        }

        internal static FontVariationCollection InterpolateCore(
            double progress,
            FontVariationCollection oldValue,
            FontVariationCollection newValue)
        {
            if (progress <= 0)
            {
                return CloneOrEmpty(oldValue);
            }

            if (progress >= 1)
            {
                return CloneOrEmpty(newValue);
            }

            if (oldValue is null && newValue is null)
            {
                return new FontVariationCollection();
            }

            var oldAxes = CreateAxisMap(oldValue);
            var newAxes = CreateAxisMap(newValue);
            var result = new FontVariationCollection(oldAxes.Count + newAxes.Count);

            foreach (var axis in oldAxes)
            {
                var from = axis.Value;
                var to = newAxes.TryGetValue(axis.Key, out var target) ? target : from;

                result.Add(new FontVariation(axis.Key, InterpolateValue(progress, from, to)));
            }

            foreach (var axis in newAxes)
            {
                if (oldAxes.ContainsKey(axis.Key))
                {
                    continue;
                }

                result.Add(new FontVariation(axis.Key, axis.Value));
            }

            return result;
        }

        private static float InterpolateValue(double progress, float oldValue, float newValue)
        {
            return (float)(((newValue - oldValue) * progress) + oldValue);
        }

        private static FontVariationCollection CloneOrEmpty(FontVariationCollection? value)
        {
            return value is null ? new FontVariationCollection() : new FontVariationCollection(value);
        }

        private static Dictionary<string, float> CreateAxisMap(FontVariationCollection? value)
        {
            var result = new Dictionary<string, float>(StringComparer.Ordinal);

            if (value is null)
            {
                return result;
            }

            foreach (var variation in value)
            {
                result[variation.Tag] = variation.Value;
            }

            return result;
        }
    }
}
