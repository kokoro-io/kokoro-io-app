using System;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms.Platform.UWP;
using WebView = Xamarin.Forms.WebView;

[assembly: ExportRenderer(typeof(MessageWebView), typeof(MessageWebViewRenderer))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class MessageWebViewRenderer : WebViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<WebView> e)
        {
            var mwv = e.OldElement as MessageWebView;

            if (mwv != null)
            {
                mwv.InvokeScriptAsyncCore = null;
                mwv.NavigateToStringCore = null;
            }

            base.OnElementChanged(e);

            mwv = e.NewElement as MessageWebView;

            if (mwv != null)
            {
                mwv.InvokeScriptAsyncCore = InvokeScriptAsyncCore;
                mwv.NavigateToStringCore = NavigateToStringCore;
            }
        }

        private Task InvokeScriptAsyncCore(string script)
            => Control.InvokeScriptAsync("eval", new[] { script }).AsTask();

        private void NavigateToStringCore(string html)
        {
            Control.NavigateToString(html);
        }
    }
}