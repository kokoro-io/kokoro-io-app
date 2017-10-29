using Android.Views;
using KokoroIO.XamarinForms.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ListView), typeof(CustomListViewRenderer))]

namespace KokoroIO.XamarinForms.Droid.Renderers
{
    public sealed class CustomListViewRenderer : ListViewRenderer
    {
        public override bool DispatchDragEvent(DragEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchDragEvent(e);
        }

        protected override bool DispatchGenericFocusedEvent(MotionEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchGenericFocusedEvent(e);
        }

        public override bool DispatchGenericMotionEvent(MotionEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchGenericMotionEvent(e);
        }

        protected override bool DispatchGenericPointerEvent(MotionEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchGenericPointerEvent(e);
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchKeyEvent(e);
        }

        protected override bool DispatchHoverEvent(MotionEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchHoverEvent(e);
        }

        public override bool DispatchTouchEvent(MotionEvent e)
        {
            if (Element.InputTransparent)
            {
                return false;
            }
            return base.DispatchTouchEvent(e);
        }
    }
}