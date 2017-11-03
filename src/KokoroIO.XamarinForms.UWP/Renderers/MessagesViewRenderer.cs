using System;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.UWP.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms.Platform.UWP;
using WebView = Xamarin.Forms.WebView;

[assembly: ExportRenderer(typeof(MessagesView), typeof(MessagesViewRenderer))]

namespace KokoroIO.XamarinForms.UWP.Renderers
{
    public sealed class MessagesViewRenderer : WebViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<WebView> e)
        {
            var mwv = e.OldElement as MessagesView;

            if (mwv != null)
            {
                mwv.InvokeScriptAsyncCore = null;
                mwv.NavigateToStringCore = null;
            }

            base.OnElementChanged(e);

            mwv = e.NewElement as MessagesView;

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