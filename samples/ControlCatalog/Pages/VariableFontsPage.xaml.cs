using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class VariableFontsPage : ContentPage
    {
        public VariableFontsPage()
        {
            InitializeComponent();

            WghtSlider.ValueChanged += OnSliderValueChanged;
            WdthSlider.ValueChanged += OnSliderValueChanged;
            SlntSlider.ValueChanged += OnSliderValueChanged;
            OpszSlider.ValueChanged += OnSliderValueChanged;
            GradSlider.ValueChanged += OnSliderValueChanged;
            XtraSlider.ValueChanged += OnSliderValueChanged;
            XopqSlider.ValueChanged += OnSliderValueChanged;
            YopqSlider.ValueChanged += OnSliderValueChanged;
            YtasSlider.ValueChanged += OnSliderValueChanged;
            YtdeSlider.ValueChanged += OnSliderValueChanged;
            YtfiSlider.ValueChanged += OnSliderValueChanged;
            YtlcSlider.ValueChanged += OnSliderValueChanged;
            YtucSlider.ValueChanged += OnSliderValueChanged;

            UpdatePreview();
            LoadMetadata();
        }

        private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void OnOpticalSizingChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var variations = new FontVariationCollection
            {
                new FontVariation("wght", (float)WghtSlider.Value),
                new FontVariation("wdth", (float)WdthSlider.Value),
                new FontVariation("slnt", (float)SlntSlider.Value),
                new FontVariation("opsz", (float)OpszSlider.Value),
                new FontVariation("GRAD", (float)GradSlider.Value),
                new FontVariation("XTRA", (float)XtraSlider.Value),
                new FontVariation("XOPQ", (float)XopqSlider.Value),
                new FontVariation("YOPQ", (float)YopqSlider.Value),
                new FontVariation("YTAS", (float)YtasSlider.Value),
                new FontVariation("YTDE", (float)YtdeSlider.Value),
                new FontVariation("YTFI", (float)YtfiSlider.Value),
                new FontVariation("YTLC", (float)YtlcSlider.Value),
                new FontVariation("YTUC", (float)YtucSlider.Value),
            };

            PreviewTextBlock.FontVariations = variations;
            PreviewTextBlock.FontOpticalSizing = OpticalSizingAuto.IsChecked == true ? FontOpticalSizing.Auto : FontOpticalSizing.None;

            WghtValue.Text = WghtSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            WdthValue.Text = WdthSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            SlntValue.Text = SlntSlider.Value.ToString("0.0", CultureInfo.InvariantCulture);
            OpszValue.Text = OpszSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            GradValue.Text = GradSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            XtraValue.Text = XtraSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            XopqValue.Text = XopqSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            YopqValue.Text = YopqSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            YtasValue.Text = YtasSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            YtdeValue.Text = YtdeSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            YtfiValue.Text = YtfiSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            YtlcValue.Text = YtlcSlider.Value.ToString("0", CultureInfo.InvariantCulture);
            YtucValue.Text = YtucSlider.Value.ToString("0", CultureInfo.InvariantCulture);

            VariationsStringTextBlock.Text = variations.ToString();
        }

        private void LoadMetadata()
        {
            try
            {
                var fontFamily = new FontFamily("avares://ControlCatalog/Assets/Fonts#Roboto Flex");
                var typeface = new Typeface(fontFamily);

                if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface) && glyphTypeface != null)
                {
                    var axes = glyphTypeface.VariationAxes;
                    if (axes.Count > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var axis in axes)
                        {
                            sb.AppendLine($"{axis.Tag}: min={axis.Min}, default={axis.Default}, max={axis.Max}, hidden={axis.IsHidden}, name={axis.DisplayName ?? "(none)"}");
                        }
                        AxesMetadataTextBlock.Text = sb.ToString().TrimEnd();
                    }
                    else
                    {
                        AxesMetadataTextBlock.Text = "No variation axes exposed by this font face.";
                    }

                    var instances = glyphTypeface.NamedInstances;
                    if (instances.Count > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var instance in instances)
                        {
                            sb.Append($"[{instance.InstanceIndex}] {instance.DisplayName ?? "(unnamed)"}");
                            if (!string.IsNullOrEmpty(instance.PostScriptName))
                            {
                                sb.Append($" / {instance.PostScriptName}");
                            }
                            sb.Append(": ");
                            sb.AppendLine(string.Join(", ", instance.Coordinates.Select(c => $"\"{c.Key}\" {c.Value.ToString(CultureInfo.InvariantCulture)}")));
                        }
                        NamedInstancesTextBlock.Text = sb.ToString().TrimEnd();
                    }
                    else
                    {
                        NamedInstancesTextBlock.Text = "No named instances exposed by this font face.";
                    }
                }
                else
                {
                    AxesMetadataTextBlock.Text = "Could not load glyph typeface for metadata.";
                    NamedInstancesTextBlock.Text = "Could not load glyph typeface for metadata.";
                }
            }
            catch (Exception ex)
            {
                AxesMetadataTextBlock.Text = $"Error loading metadata: {ex.Message}";
                NamedInstancesTextBlock.Text = $"Error loading metadata: {ex.Message}";
            }
        }
    }
}
