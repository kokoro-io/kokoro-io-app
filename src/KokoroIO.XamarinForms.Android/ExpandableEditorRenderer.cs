using System.ComponentModel;
using Android.Views;
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

        private void Control_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            UpdateSelection();
        }

        private void Control_KeyPress(object sender, KeyEventArgs e)
        {
            UpdateSelection();
            e.Handled = false;
        }

        private void UpdateSelection()
        {
            var ee = Element as ExpandableEditor;
            if (ee != null)
            {
                ee.SelectionStart = Control.SelectionStart;
                ee.SelectionLength = Control.SelectionEnd - Control.SelectionStart;
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
                        if (Element is ExpandableEditor ee)
                        {
                            if (!_IsTouching
                                && 0 <= ee.SelectionStart
                                && ee.SelectionLength >= 0
                                && ee.SelectionStart + ee.SelectionLength < Control.Text.Length)
                            {
                                Control.SetSelection(ee.SelectionStart, ee.SelectionStart + ee.SelectionLength);
                            }
                        }
                        break;
                }
            }

            base.OnElementPropertyChanged(sender, e);
        }

        private bool _IsTouching;

        public override bool OnTouchEvent(MotionEvent e)
        {
            _IsTouching = e.Action == MotionEventActions.Up;
            return base.OnTouchEvent(e);
        }
    }
}