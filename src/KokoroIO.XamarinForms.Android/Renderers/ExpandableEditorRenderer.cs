using System;
using System.ComponentModel;
using Android.Views;
using KokoroIO.XamarinForms.Droid.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ExpandableEditor), typeof(ExpandableEditorRenderer))]

namespace KokoroIO.XamarinForms.Droid.Renderers
{
    public sealed class ExpandableEditorRenderer : EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.Hint = (e.NewElement as ExpandableEditor)?.Placeholder ?? string.Empty;

                // Xamarin.Forms.Platform.Android did not provide events to handle selection change.
                Control.TextChanged += Control_TextChanged;
                Control.KeyPress += Control_KeyPress;
                Control.Click += Control_Click;
            }
        }

        private void Control_Click(object sender, System.EventArgs e)
        {
            UpdateSelection();
        }

        private int _PreviousLineCount = 1;

        private void Control_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            UpdateSelection();

            if (Control?.LineCount > 0)
            {
                var ee = Element as ExpandableEditor;
                var lc = Math.Min(Control.LineCount, ee?.MaxLines > 0 ? ee.MaxLines : int.MaxValue);

                if (lc != _PreviousLineCount)
                {
                    _PreviousLineCount = lc;
                    Control.SetLines(lc);
                    ee?.InvalidateMeasure();
                }
            }
        }

        private void Control_KeyPress(object sender, KeyEventArgs e)
        {
            UpdateSelection();
            e.Handled = false;
        }

        private bool _IsSelectionUpdating;

        private void UpdateSelection()
        {
            var ee = Element as ExpandableEditor;
            if (ee != null)
            {
                _IsSelectionUpdating = true;
                ee.SelectionStart = Control.SelectionStart;
                ee.SelectionLength = Control.SelectionEnd - Control.SelectionStart;
                _IsSelectionUpdating = false;
                ApplySelection();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Control != null)
            {
                switch (e.PropertyName)
                {
                    case nameof(ExpandableEditor.Placeholder):

                        Control.Hint = (Element as ExpandableEditor)?.Placeholder ?? string.Empty;

                        break;

                    case nameof(ExpandableEditor.SelectionStart):
                    case nameof(ExpandableEditor.SelectionLength):
                        ApplySelection();
                        break;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }

        private void ApplySelection()
        {
            if (Element is ExpandableEditor ee)
            {
                if (!_IsTouching
                    && !_IsSelectionUpdating
                    && 0 <= ee.SelectionStart
                    && ee.SelectionLength >= 0
                    && ee.SelectionStart + ee.SelectionLength < Control.Text.Length
                    && (ee.SelectionStart != Control.SelectionStart
                        || ee.SelectionLength != Control.SelectionEnd - Control.SelectionStart))
                {
                    Control.SetSelection(ee.SelectionStart, ee.SelectionStart + ee.SelectionLength);
                }
            }
        }

        private bool _IsTouching;

        public override bool OnTouchEvent(MotionEvent e)
        {
            _IsTouching = e.Action == MotionEventActions.Up;
            return base.OnTouchEvent(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Control != null)
                {
                    Control.TextChanged -= Control_TextChanged;
                    Control.KeyPress -= Control_KeyPress;
                    Control.Click -= Control_Click;
                }
            }
            base.Dispose(disposing);
        }
    }
}