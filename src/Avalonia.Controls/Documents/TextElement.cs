using Avalonia.Media;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// TextElement is an  base class for content in text based controls.
    /// TextElements span other content, applying property values or providing structural information.
    /// </summary>
    public abstract class TextElement : StyledElement
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextElement>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontFamily>(
                nameof(FontFamily),
                defaultValue: FontFamily.Default,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontFeatures"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFeatureCollection?> FontFeaturesProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontFeatureCollection?>(
                nameof(FontFeatures),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontVariations"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontVariationCollection?> FontVariationsProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontVariationCollection?>(
                nameof(FontVariations),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontVariationNamedInstance"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontVariationNamedInstance?> FontVariationNamedInstanceProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontVariationNamedInstance?>(
                nameof(FontVariationNamedInstance),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontOpticalSizing"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontOpticalSizing> FontOpticalSizingProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontOpticalSizing>(
                nameof(FontOpticalSizing),
                defaultValue: FontOpticalSizing.Auto,
                inherits: true);
        
        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, double>(
                nameof(FontSize),
                defaultValue: 12,
                inherits: true,
                validate: fontSize => fontSize > 0 && !double.IsNaN(fontSize) && !double.IsInfinity(fontSize));

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="FontStretch"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStretch> FontStretchProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, FontStretch>(
                nameof(FontStretch),
                inherits: true,
                defaultValue: FontStretch.Normal);

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush?> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<TextElement, TextElement, IBrush?>(
                nameof(Foreground),
                Brushes.Black,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="LetterSpacing"/> property.
        /// </summary>
        /// <remarks>
        /// This is an inherited attached property that defines letter spacing for text.
        /// Letter spacing is specified in pixels. Default value is 0 (normal spacing).
        /// Positive values increase spacing between characters.
        /// Negative values decrease spacing between characters.
        /// </remarks>
        public static readonly AttachedProperty<double> LetterSpacingProperty =
            AvaloniaProperty.RegisterAttached<TextElement, Control, double>(
                name: nameof(LetterSpacing),
                defaultValue: 0.0,
                inherits: true);

        private IInlineHost? _inlineHost;

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font features.
        /// </summary>
        public FontFeatureCollection? FontFeatures
        {
            get => GetValue(FontFeaturesProperty);
            set => SetValue(FontFeaturesProperty, value);
        }

        /// <summary>
        /// Gets or sets the variable font axis coordinates inherited by text content.
        /// </summary>
        /// <remarks>
        /// Values use CSS <c>font-variation-settings</c> item syntax through <see cref="FontVariationCollection"/>.
        /// Explicit entries override matching coordinates from a named instance, font weight, stretch, italic style,
        /// and automatic optical sizing. Unsupported axes are ignored by font faces that do not expose matching tags.
        /// Changes invalidate text shaping, so animated variation values produce newly shaped glyph runs.
        /// </remarks>
        public FontVariationCollection? FontVariations
        {
            get => GetValue(FontVariationsProperty);
            set => SetValue(FontVariationsProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected variable font named instance inherited by text content.
        /// </summary>
        /// <remarks>
        /// The named instance is matched against the selected font face by instance index and PostScript name.
        /// When it resolves, its coordinates are used as the base variation values and can still be overridden by
        /// inherited font properties, automatic optical sizing, and explicit <see cref="FontVariations"/> entries.
        /// </remarks>
        public FontVariationNamedInstance? FontVariationNamedInstance
        {
            get => GetValue(FontVariationNamedInstanceProperty);
            set => SetValue(FontVariationNamedInstanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the inherited optical sizing behavior for variable fonts.
        /// </summary>
        /// <remarks>
        /// <see cref="Media.FontOpticalSizing.Auto"/> supplies the current font size as the <c>opsz</c> coordinate
        /// only when the font face supports that axis and <see cref="FontVariations"/> does not contain an explicit
        /// <c>"opsz"</c> entry. <see cref="Media.FontOpticalSizing.None"/> disables only the automatic coordinate;
        /// explicit <c>"opsz"</c> entries are still applied.
        /// </remarks>
        public FontOpticalSizing FontOpticalSizing
        {
            get => GetValue(FontOpticalSizingProperty);
            set => SetValue(FontOpticalSizingProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the font stretch.
        /// </summary>
        public FontStretch FontStretch
        {
            get => GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the letter spacing.
        /// </summary>
        public double LetterSpacing
        {
            get => GetValue(LetterSpacingProperty);
            set => SetValue(LetterSpacingProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontFamily GetFontFamily(Control control)
        {
            return control.GetValue(FontFamilyProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFamilyProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontFamily(Control control, FontFamily value)
        {
            control.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontFeaturesProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font family.</returns>
        public static FontFeatureCollection? GetFontFeatures(Control control)
        {
            return control.GetValue(FontFeaturesProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontFeaturesProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontFeatures(Control control, FontFeatureCollection? value)
        {
            control.SetValue(FontFeaturesProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontVariationsProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font variations.</returns>
        public static FontVariationCollection? GetFontVariations(Control control)
        {
            return control.GetValue(FontVariationsProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontVariationsProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontVariations(Control control, FontVariationCollection? value)
        {
            control.SetValue(FontVariationsProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontVariationNamedInstanceProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The selected font variation named instance.</returns>
        public static FontVariationNamedInstance? GetFontVariationNamedInstance(Control control)
        {
            return control.GetValue(FontVariationNamedInstanceProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontVariationNamedInstanceProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontVariationNamedInstance(Control control, FontVariationNamedInstance? value)
        {
            control.SetValue(FontVariationNamedInstanceProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontOpticalSizingProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font optical sizing behavior.</returns>
        public static FontOpticalSizing GetFontOpticalSizing(Control control)
        {
            return control.GetValue(FontOpticalSizingProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontOpticalSizingProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontOpticalSizing(Control control, FontOpticalSizing value)
        {
            control.SetValue(FontOpticalSizingProperty, value);
        }
        
        /// <summary>
        /// Gets the value of the attached <see cref="LetterSpacingProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The letter spacing applied to the control.</returns>
        public static double GetLetterSpacing(Control control)
        {
            return control.GetValue(LetterSpacingProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="LetterSpacingProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The letter spacing to apply.</param>
        public static void SetLetterSpacing(Control control, double value)
        {
            control.SetValue(LetterSpacingProperty, value);
        }
        
        /// <summary>
        /// Gets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font size.</returns>
        public static double GetFontSize(Control control)
        {
            return control.GetValue(FontSizeProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontSizeProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontSize(Control control, double value)
        {
            control.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font style.</returns>
        public static FontStyle GetFontStyle(Control control)
        {
            return control.GetValue(FontStyleProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontStyleProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontStyle(Control control, FontStyle value)
        {
            control.SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font weight.</returns>
        public static FontWeight GetFontWeight(Control control)
        {
            return control.GetValue(FontWeightProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontWeightProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontWeight(Control control, FontWeight value)
        {
            control.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="FontStretchProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font stretch.</returns>
        public static FontStretch GetFontStretch(Control control)
        {
            return control.GetValue(FontStretchProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="FontStretchProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFontStretch(Control control, FontStretch value)
        {
            control.SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The foreground.</returns>
        public static IBrush? GetForeground(Control control)
        {
            return control.GetValue(ForegroundProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="ForegroundProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetForeground(Control control, IBrush? value)
        {
            control.SetValue(ForegroundProperty, value);
        }

        internal IInlineHost? InlineHost
        {
            get => _inlineHost;
            set
            {
                var oldValue = _inlineHost;
                _inlineHost = value;
                OnInlineHostChanged(oldValue, value);
            }
        }

        internal virtual void OnInlineHostChanged(IInlineHost? oldValue, IInlineHost? newValue)
        {

        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(Background):
                case nameof(FontFamily):
                case nameof(FontSize):
                case nameof(FontStyle):
                case nameof(FontWeight):
                case nameof(FontStretch):
                case nameof(FontFeatures):
                case nameof(FontVariations):
                case nameof(FontVariationNamedInstance):
                case nameof(FontOpticalSizing):
                case nameof(Foreground):
                    InlineHost?.Invalidate();
                    break;
            }
        }
    }
}
