namespace Avalonia.Media;

/// <summary>
/// Controls whether optical sizing is automatically applied to variable fonts that support the <c>opsz</c> axis.
/// </summary>
/// <remarks>
/// When automatic optical sizing is enabled, Avalonia supplies the current font size as the <c>opsz</c>
/// coordinate only if the selected font face exposes that axis and no explicit <c>"opsz"</c> entry is present
/// in <see cref="FontVariationCollection"/>. The value participates in normal variation animation because the
/// effective coordinate is recomputed when font size, optical sizing, named instance, or variation settings change.
/// Static fonts and variable fonts without an <c>opsz</c> axis are unaffected.
/// </remarks>
public enum FontOpticalSizing
{
    /// <summary>
    /// The <c>opsz</c> axis is automatically set from the current font size when the font face supports it.
    /// This is the default.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Optical sizing is not automatically applied. Explicit <c>"opsz"</c> settings in
    /// <see cref="FontVariationCollection"/> are still respected.
    /// </summary>
    None = 1
}
