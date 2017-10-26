using System.ComponentModel;
using KokoroIO.XamarinForms.iOS;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(RoundLabel), typeof(RoundLabelRenderer))]

namespace KokoroIO.XamarinForms.iOS
{
    public class RoundLabelRenderer : LabelRenderer
    {
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(RoundLabel.BackgroundColor):
                case nameof(RoundLabel.BorderColor):
                case nameof(RoundLabel.BorderWidth):
                case nameof(RoundLabel.CornerRadius):
                    SetNeedsLayout();
                    break;
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            var label = Element as RoundLabel;

            if (label != null)
            {
                Layer.BorderWidth = label.BorderWidth;
                Layer.CornerRadius = label.CornerRadius;
                Layer.BackgroundColor = label.BackgroundColor.ToCGColor();
                Layer.BorderColor = label.BorderColor.ToCGColor();
            }
        }
    }
}