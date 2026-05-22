using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Media
{
    /// <summary>
    /// Creates a <see cref="FontVariationCollection"/> from a string representation.
    /// </summary>
    public class FontVariationCollectionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            return value is string s ? FontVariationCollection.Parse(s) : null;
        }
    }
}
