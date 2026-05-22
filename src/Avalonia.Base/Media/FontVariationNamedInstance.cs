using System.Collections.Generic;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes a named design-space instance exposed by a variable font face.
    /// </summary>
    /// <remarks>
    /// A named instance is identified for a specific face first by <paramref name="InstanceIndex"/> when it is in
    /// range and, when both values are available, its <paramref name="PostScriptName"/> matches the instance at
    /// that index. If the index does not match, Avalonia falls back to an ordinal <paramref name="PostScriptName"/>
    /// lookup. If neither lookup succeeds, the requested named instance is ignored for that face. Coordinates from
    /// the resolved named instance provide the base variation values and can be overridden by font weight, stretch,
    /// italic style, automatic optical sizing, and explicit <see cref="FontVariationCollection"/> entries.
    /// </remarks>
    /// <param name="InstanceIndex">The zero-based named instance index in the font face.</param>
    /// <param name="DisplayName">The localized display name for the named instance, if available.</param>
    /// <param name="PostScriptName">The PostScript name for the named instance, if available.</param>
    /// <param name="Coordinates">The axis design coordinates that define the named instance.</param>
    public readonly record struct FontVariationNamedInstance(
        int InstanceIndex,
        string? DisplayName,
        string? PostScriptName,
        IReadOnlyList<KeyValuePair<string, float>> Coordinates);
}
