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
                mwv.UpdateAsync = null;
            }

            base.OnElementChanged(e);

            mwv = e.NewElement as MessagesView;

            if (mwv != null)
            {
                mwv.UpdateAsync = UpdateAsync;
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
                    Control.NavigateToString(MessagesViewHelper.GetHtml());
                }
                var script = MessagesViewHelper.CreateScriptForRequest(mwv.Messages, reset, requests, hasUnread);
                if (!string.IsNullOrEmpty(script))
                {
                    await Control.InvokeScriptAsync("eval", new[] { script });
                    return true;
                }
            }
            return false;
        }
    }
}