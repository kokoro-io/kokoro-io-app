using System.ComponentModel;
using KokoroIO.XamarinForms.Droid;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ExpandableEditor), typeof(ExpandableEditorRenderer))]

namespace KokoroIO.XamarinForms.Droid
{
    public sealed class ExpandableEditorRenderer : EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.Hint = (e.NewElement as ExpandableEditor)?.Placeholder ?? string.Empty;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExpandableEditor.Placeholder))
            {
                if (Control != null)
                {
                    Control.Hint = (Element as ExpandableEditor)?.Placeholder ?? string.Empty;
                }
            }
            base.OnElementPropertyChanged(sender, e);
        }
    }
}