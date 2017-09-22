using System.ComponentModel;
using Foundation;
using KokoroIO.XamarinForms.iOS;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ExpandableEditor), typeof(ExpandableEditorRenderer))]

namespace KokoroIO.XamarinForms.iOS
{
    public sealed class ExpandableEditorRenderer : EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.SelectionChanged += Control_SelectionChanged;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Control != null)
            {
                switch (e.PropertyName)
                {
                    case nameof(ExpandableEditor.SelectionStart):
                    case nameof(ExpandableEditor.SelectionLength):
                        if (Element is ExpandableEditor ee)
                        {
                            if (0 <= ee.SelectionStart
                                && ee.SelectionLength >= 0
                                && ee.SelectionStart + ee.SelectionLength < Control.Text.Length)
                            {
                                Control.SelectedRange = new NSRange(ee.SelectionStart, ee.SelectionLength);
                            }
                        }
                        break;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }

        private void Control_SelectionChanged(object sender, System.EventArgs e)
        {
            var ee = Element as ExpandableEditor;
            if (ee != null)
            {
                ee.SelectionStart = (int)Control.SelectedRange.Location;
                ee.SelectionLength = (int)Control.SelectedRange.Length;
            }
        }

        // TODO: implement placeholder

        //protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        //{
        //    base.OnElementChanged(e);

        //    if (Control != null)
        //    {
        //        Control.PlaceholderText = (e.NewElement as ExpandableEditor)?.Placeholder ?? string.Empty;
        //    }
        //}

        //protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == nameof(ExpandableEditor.Placeholder))
        //    {
        //        if (Control != null)
        //        {
        //            Control.PlaceholderText = (Element as ExpandableEditor)?.Placeholder ?? string.Empty;
        //        }
        //    }
        //    base.OnElementPropertyChanged(sender, e);
        //}
    }
}