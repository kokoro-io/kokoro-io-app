using System.ComponentModel;
using KokoroIO.XamarinForms.Droid.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(RoundLabel), typeof(RoundLabelRenderer))]

namespace KokoroIO.XamarinForms.Droid.Renderers
{
    public class RoundLabelRenderer : LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == null)
            {
                return;
            }

            if (Control != null)
            {
                UpdateMinimumWidth();

                Control.Invalidate();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(Label.Text):
                case nameof(RoundLabel.BackgroundColor):
                case nameof(RoundLabel.BorderColor):
                case nameof(RoundLabel.BorderWidth):
                case nameof(RoundLabel.CornerRadius):
                    Control.Invalidate();
                    break;

                case nameof(Label.MinimumHeightRequest):
                    UpdateMinimumWidth();
                    break;
            }
        }

        private void UpdateMinimumWidth()
        {
            var v = (int)System.Math.Round(Element.MinimumWidthRequest);
            Control.SetMinWidth(v);
            Control.SetMinimumWidth(v);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var label = Element as RoundLabel;

            if (label != null)
            {
                var background = new Android.Graphics.Drawables.GradientDrawable();
                background.SetCornerRadius((int)label.CornerRadius);
                background.SetStroke((int)label.BorderWidth, label.BorderColor.ToAndroid());
                background.SetColor(label.BackgroundColor.ToAndroid());
                SetBackgroundColor(Android.Graphics.Color.Transparent);
                SetBackgroundDrawable(background);
            }
        }
    }
}