using System;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

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
            }

            base.OnElementChanged(e);

            mwv = e.NewElement as MessageWebView;

            if (mwv != null)
            {
                mwv.InvokeScriptAsyncCore = InvokeScriptAsyncCore;
            }
        }

        private async Task InvokeScriptAsyncCore(string script)
        {
            await Control.InvokeScriptAsync("eval", new[] { script });
        }
    }
}