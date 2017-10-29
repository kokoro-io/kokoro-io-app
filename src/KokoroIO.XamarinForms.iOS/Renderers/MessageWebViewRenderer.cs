using Foundation;
using KokoroIO.XamarinForms.iOS.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(MessageWebView), typeof(MessageWebViewRenderer))]

namespace KokoroIO.XamarinForms.iOS.Renderers
{
    public sealed class MessageWebViewRenderer : WebViewRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            var mwv = e.OldElement as MessageWebView;

            if (mwv != null)
            {
                mwv.NavigateToStringCore = null;
            }

            base.OnElementChanged(e);

            mwv = e.NewElement as MessageWebView;

            if (mwv != null)
            {
                mwv.NavigateToStringCore = NavigateToStringCore;
            }
        }

        private void NavigateToStringCore(string html)
        {
            LoadHtmlString(html, new NSUrl(NSBundle.MainBundle.BundlePath, true));
        }
    }
}