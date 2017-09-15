using System;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.UWP;
using KokoroIO.XamarinForms.Views;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;
using WebView = Xamarin.Forms.WebView;
using WWebView = Windows.UI.Xaml.Controls.WebView;

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

        private async Task InvokeScriptAsyncCore(string script)
        {
            await Control.InvokeScriptAsync("eval", new[] { script });
        }

        private void NavigateToStringCore(string html)
        {
            Control.NavigateToString(html);
        }
    }
}