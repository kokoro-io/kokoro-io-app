using System.Threading.Tasks;
using Foundation;
using KokoroIO.XamarinForms.iOS.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(MessagesView), typeof(MessagesViewRenderer))]

namespace KokoroIO.XamarinForms.iOS.Renderers
{
    public sealed class MessagesViewRenderer : WebViewRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
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

        private Task<bool> UpdateAsync(bool reset, MessagesViewUpdateRequest[] requests, bool? hasUnread)
        {

            var mwv = Element as MessagesView;

            if (mwv != null)
            {
                if (!_Loaded)
                {
                    _Loaded = true;
                    LoadHtmlString(MessagesViewHelper.GetHtml(), new NSUrl(NSBundle.MainBundle.BundlePath, true));
                }

                var script = MessagesViewHelper.CreateScriptForRequest(mwv.Messages, reset, requests, hasUnread);
                if (!string.IsNullOrEmpty(script))
                {
                    EvaluateJavascript(script);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}