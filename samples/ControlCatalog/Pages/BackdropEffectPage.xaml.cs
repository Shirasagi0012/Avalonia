using Avalonia.Controls;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class BackdropEffectPage : ContentPage
    {
        public BackdropEffectPage()
        {
            InitializeComponent();
            UpdateBackdropEffects();
            BlurRadiusSlider.ValueChanged += (_, _) => UpdateBackdropEffects();
        }

        private void UpdateBackdropEffects()
        {
            var radius = BlurRadiusSlider.Value;
            var effect = new BlurEffect { Radius = radius };

            FrostedCard.BackdropEffect = effect;
            BlurOverlayBar.BackdropEffect = effect;
            BlurRadiusLabel.Text = $"blur({radius:0}px)";
        }
    }
}
