using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : FontCollectionBase
    {
        private readonly IFontManagerImpl _platformImpl;

        public SystemFontCollection(IFontManagerImpl platformImpl)
        {
            _platformImpl = platformImpl ?? throw new ArgumentNullException(nameof(platformImpl));

            var familyNames = _platformImpl.GetInstalledFontFamilyNames().Where(x => !string.IsNullOrEmpty(x));

            foreach (var familyName in familyNames)
            {
                AddFontFamily(familyName);
            }
        }

        public override Uri Key => FontManager.SystemFontsKey;

        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            return TryGetGlyphTypeface(new Typeface(familyName, style, weight, stretch), out glyphTypeface);
        }

        public override bool TryGetGlyphTypeface(Typeface typeface, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var normalizedTypeface = typeface.Normalize(out var familyName);
            var key = normalizedTypeface.ToFontCollectionKey();

            // Find an exact match first
            if (TryGetGlyphTypeface(familyName, key, allowNearestMatch: false, out glyphTypeface))
            {
                return true;
            }

            //Check cache first to avoid unnecessary calls to the font manager
            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) && glyphTypefaces.TryGetValue(key, out glyphTypeface))
            {
                return glyphTypeface != null;
            }

            //Try to create the glyph typeface via system font manager
            if (!_platformImpl.TryCreateGlyphTypeface(
                    familyName,
                    normalizedTypeface.Style,
                    normalizedTypeface.Weight,
                    normalizedTypeface.Stretch,
                    out var platformTypeface))
            {
                //Add null to cache to avoid future calls
                TryAddGlyphTypeface(familyName, key, null);

                return false;
            }

            // The font manager didn't return a perfect match either. Find the nearest match ourselves.
            if (key != platformTypeface.ToFontCollectionKey() &&
                TryGetGlyphTypeface(familyName, key, allowNearestMatch: true, out glyphTypeface))
            {
                return true;
            }

            glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
            if (glyphTypeface is null)
            {
                return false;
            }

            //Add to cache with platform typeface family name first
            TryAddGlyphTypeface(platformTypeface.FamilyName, key, glyphTypeface);

            //Add to cache
            if (!TryAddGlyphTypeface(glyphTypeface))
            {
                // Another thread may have added an entry for this key while we were creating the glyph typeface.
                // Re-check the cache and yield the existing glyph typeface if present.
                if (_glyphTypefaceCache.TryGetValue(familyName, out var existingMap) && existingMap.TryGetValue(key, out var existingTypeface) && existingTypeface != null)
                {
                    glyphTypeface = existingTypeface;

                    return true;
                }

                return false;
            }

            //Requested glyph typeface should be in cache now
            return TryGetGlyphTypeface(familyName, key, allowNearestMatch: false, out glyphTypeface);
        }

        internal override bool TryGetGlyphTypeface(
            Typeface typeface,
            EffectiveVariationCoordinates? variationCoordinates,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var normalizedTypeface = typeface.Normalize(out var familyName);
            var key = normalizedTypeface.ToFontCollectionKey(variationCoordinates);

            // Find an exact match first
            if (TryGetGlyphTypeface(familyName, key, allowNearestMatch: false, out glyphTypeface))
            {
                return true;
            }

            if (key.HasVariationCoordinates && _platformImpl is IFontManagerImplWithVariations variationPlatformImpl)
            {
                if (!variationPlatformImpl.TryCreateGlyphTypeface(
                        familyName,
                        normalizedTypeface.Style,
                        normalizedTypeface.Weight,
                        normalizedTypeface.Stretch,
                        key.VariationCoordinates!,
                        out var variationPlatformTypeface))
                {
                    TryAddGlyphTypeface(familyName, key, null);

                    return false;
                }

                glyphTypeface = GlyphTypeface.TryCreate(variationPlatformTypeface);
                if (glyphTypeface is null)
                {
                    return false;
                }

                TryAddGlyphTypeface(variationPlatformTypeface.FamilyName, key, glyphTypeface);

                if (!TryAddGlyphTypeface(glyphTypeface, key))
                {
                    if (_glyphTypefaceCache.TryGetValue(familyName, out var existingVariationMap) &&
                        existingVariationMap.TryGetValue(key, out var existingVariationTypeface) &&
                        existingVariationTypeface != null)
                    {
                        glyphTypeface = existingVariationTypeface;

                        return true;
                    }

                    return false;
                }

                return TryGetGlyphTypeface(familyName, key, allowNearestMatch: false, out glyphTypeface);
            }

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) &&
                glyphTypefaces.TryGetValue(key.WithoutVariationCoordinates(), out glyphTypeface))
            {
                if (glyphTypeface != null && key.HasVariationCoordinates)
                {
                    TryAddGlyphTypeface(familyName, key, glyphTypeface);
                }

                return glyphTypeface != null;
            }

            if (!_platformImpl.TryCreateGlyphTypeface(familyName, normalizedTypeface.Style, normalizedTypeface.Weight,
                    normalizedTypeface.Stretch, out var platformTypeface))
            {
                TryAddGlyphTypeface(familyName, key, null);

                return false;
            }

            var platformKey = platformTypeface.ToFontCollectionKey();

            if (key.WithoutVariationCoordinates() != platformKey &&
                TryGetGlyphTypeface(familyName, key, allowNearestMatch: true, out glyphTypeface))
            {
                return true;
            }

            glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
            if (glyphTypeface is null)
            {
                return false;
            }

            TryAddGlyphTypeface(platformTypeface.FamilyName, key, glyphTypeface);

            if (!TryAddGlyphTypeface(glyphTypeface, platformKey))
            {
                if (_glyphTypefaceCache.TryGetValue(familyName, out var existingMap) &&
                    existingMap.TryGetValue(key.WithoutVariationCoordinates(), out var existingTypeface) &&
                    existingTypeface != null)
                {
                    glyphTypeface = existingTypeface;

                    if (key.HasVariationCoordinates)
                    {
                        TryAddGlyphTypeface(familyName, key, glyphTypeface);
                    }

                    return true;
                }

                return false;
            }

            return TryGetGlyphTypeface(familyName, key, allowNearestMatch: false, out glyphTypeface);
        }

        public override bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            return _platformImpl.TryGetFamilyTypefaces(familyName, out familyTypefaces);
        }

        public override bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName,
           CultureInfo? culture, out Typeface match)
        {
            var typeface = new Typeface(
                familyName is null ? FontFamily.Default : new FontFamily(familyName),
                style,
                weight,
                stretch);

            return TryMatchCharacter(codepoint, typeface, culture, out match);
        }

        public override bool TryMatchCharacter(int codepoint, Typeface typeface, CultureInfo? culture, out Typeface match)
        {
            var requestedTypeface = typeface.Normalize(out var familyName);
            var requestedKey = requestedTypeface.ToFontCollectionKey();

            if (base.TryMatchCharacter(codepoint, requestedTypeface, culture, out match))
            {
                var matchKey = match.ToFontCollectionKey();

                if (requestedKey == matchKey)
                {
                    return true;
                }
            }

            if (_platformImpl.TryMatchCharacter(
                    codepoint,
                    requestedTypeface.Style,
                    requestedTypeface.Weight,
                    requestedTypeface.Stretch,
                    familyName,
                    culture,
                    out var platformTypeface))
            {
                // Construct the resulting Typeface
                match = new Typeface(platformTypeface.FamilyName, platformTypeface.Style, platformTypeface.Weight,
                       platformTypeface.Stretch, requestedTypeface.Variations, requestedTypeface.OpticalSizing,
                       requestedTypeface.NamedInstance);

                // Compute the key for cache lookup this can be different from the requested key
                var key = match.ToFontCollectionKey();

                // Check cache first: if an entry exists and is non-null, match succeeded and we can return true.
                if (_glyphTypefaceCache.TryGetValue(platformTypeface.FamilyName, out var glyphTypefaces) && glyphTypefaces.TryGetValue(key, out var existing))
                {
                    return existing != null;
                }

                // Not in cache yet: create glyph typeface and try to add it.
                if (GlyphTypeface.TryCreate(platformTypeface) is not { } glyphTypeface)
                {
                    return false;
                }

                // Try adding with the platform typeface family name first.
                TryAddGlyphTypeface(platformTypeface.FamilyName, key, glyphTypeface);

                // Try adding the glyph typeface with the matched key.
                if (TryAddGlyphTypeface(glyphTypeface, key))
                {
                    return true;
                }

                // TryAddGlyphTypeface failed: another thread may have added an entry. Re-check the cache.
                if (_glyphTypefaceCache.TryGetValue(platformTypeface.FamilyName, out glyphTypefaces) && glyphTypefaces.TryGetValue(key, out existing))
                {
                    return existing != null;
                }

                return false;
            }

            return false;
        }
    }
}
