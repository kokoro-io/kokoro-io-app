using System;
using System.ComponentModel;
using Foundation;
using KokoroIO.XamarinForms.iOS.Renderers;
using KokoroIO.XamarinForms.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ExpandableEditor), typeof(ExpandableEditorRenderer))]

namespace KokoroIO.XamarinForms.iOS.Renderers
{
    public sealed class ExpandableEditorRenderer : EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.Changed += Control_Changed;
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
                            if (!_IsTouching
                                && 0 <= ee.SelectionStart
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

        private int _PreviousLineCount = 1;

        private void Control_Changed(object sender, System.EventArgs e)
        {
            var ee = Element as ExpandableEditor;
            var rlc = Control.ContentSize.Height / Control.Font.LineHeight;
            var lc = Math.Min((int)Math.Round(rlc), ee?.MaxLines > 0 ? ee.MaxLines : int.MaxValue);

            if (lc != _PreviousLineCount)
            {
                _PreviousLineCount = lc;
                Element.MinimumHeightRequest = lc * Control.Font.LineHeight;
                ee?.InvalidateMeasure();
            }
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

        private bool _IsTouching;

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            _IsTouching = true;
            base.TouchesBegan(touches, evt);
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);
            _IsTouching = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Control != null)
                {
                    Control.Changed -= Control_Changed;
                    Control.SelectionChanged -= Control_SelectionChanged;
                }
            }
            base.Dispose(disposing);
        }

        // TODO: implement placeholder
    }
}