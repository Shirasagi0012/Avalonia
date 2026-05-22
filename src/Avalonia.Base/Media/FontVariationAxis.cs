namespace Avalonia.Media
{
    /// <summary>
    /// Describes a variation axis supported by a font face.
    /// </summary>
    /// <remarks>
    /// Axis metadata is reported by the selected font face. Rendering coordinates for matching tags are clamped
    /// to <paramref name="Min"/> and <paramref name="Max"/>. Axes marked by <paramref name="IsHidden"/> are still
    /// valid for rendering; the flag indicates that font authors do not intend the axis to be shown in ordinary
    /// user-facing axis pickers.
    /// </remarks>
    /// <param name="Tag">The case-sensitive four-character OpenType variation axis tag.</param>
    /// <param name="Min">The minimum design coordinate value supported by the axis.</param>
    /// <param name="Default">The default design coordinate value for the axis.</param>
    /// <param name="Max">The maximum design coordinate value supported by the axis.</param>
    /// <param name="IsHidden">Whether the axis is hidden from ordinary user-facing selection.</param>
    /// <param name="DisplayName">The localized display name for the axis, if available.</param>
    public readonly record struct FontVariationAxis(
        string Tag,
        float Min,
        float Default,
        float Max,
        bool IsHidden,
        string? DisplayName = null);
}
