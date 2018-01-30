using System;
using System.Threading.Tasks;
using Foundation;
using KokoroIO.XamarinForms.iOS.Renderers;
using KokoroIO.XamarinForms.Views;
using WebKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(MessagesView), typeof(MessagesViewRenderer))]

namespace KokoroIO.XamarinForms.iOS.Renderers
{
    public sealed class MessagesViewRenderer : ViewRenderer<MessagesView, WKWebView>
    {
        private class CustomDelegate : WKNavigationDelegate
        {
            private readonly MessagesViewRenderer _Renderer;

            public CustomDelegate(MessagesViewRenderer r)
            {
                _Renderer = r;
            }

            public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
            {
                if (_Renderer.OnNavigating(navigationAction.Request.Url.ToString()))
                {
                    decisionHandler(WKNavigationActionPolicy.Cancel);
                }
                else
                {
                    decisionHandler(WKNavigationActionPolicy.Allow);
                }
            }
        }

        private bool OnNavigating(string url)
        {
            if (url.StartsWith("file://", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                return MessagesViewHelper.ShouldOverrideRequest(Element as MessagesView, url);
            }
            catch
            { }

            return false;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<MessagesView> e)
        {
            if (e.OldElement != null)
            {
                e.OldElement.UpdateAsync = null;
            }

            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(new WKWebView(Frame, new WKWebViewConfiguration() { }));

                Control.NavigationDelegate = new CustomDelegate(this);
            }

            if (e.NewElement != null)
            {
                e.NewElement.UpdateAsync = UpdateAsync;
            }
        }

        private bool _Loaded;

        private async Task<bool> UpdateAsync(bool reset, MessagesViewUpdateRequest[] requests, bool? hasUnread)
        {
            var mwv = Element as MessagesView;

            if (mwv != null)
            {
                if (!_Loaded)
                {
                    _Loaded = true;
                    Control.LoadHtmlString(MessagesViewHelper.GetHtml(), new NSUrl(NSBundle.MainBundle.BundlePath, true));
                }

                var script = MessagesViewHelper.CreateScriptForRequest(mwv.Messages, reset, requests, hasUnread);
                if (!string.IsNullOrEmpty(script))
                {
                    await Control.EvaluateJavaScriptAsync(script);
                    return true;
                }
            }
            return false;
        }
    }
}