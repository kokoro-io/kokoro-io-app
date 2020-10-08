using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Webkit;
using KokoroIO.XamarinForms.Droid.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using AWebView = Android.Webkit.WebView;

[assembly: ExportRenderer(typeof(MessagesView), typeof(MessagesViewRenderer))]

namespace KokoroIO.XamarinForms.Droid.Renderers
{
    public class MessagesViewRenderer : WebViewRenderer
    {
        private class WebClient : WebViewClient
        {
            private MessagesViewRenderer _renderer;

            public WebClient(MessagesViewRenderer renderer)
            {
                _renderer = renderer;
            }

            public override bool ShouldOverrideUrlLoading(AWebView view, IWebResourceRequest request)
            {
                if (_renderer.Element == null)
                {
                    return true;
                }

                return _renderer.OnNavigating(request.Url?.ToString());
            }

            [Obsolete]
            public override bool ShouldOverrideUrlLoading(AWebView view, string url)
            {
                if (_renderer.Element == null)
                {
                    return true;
                }

                return _renderer.OnNavigating(url);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    _renderer = null;
                }
            }
        }

        public MessagesViewRenderer(Context context)
            : base(context) { }

        static MessagesViewRenderer()
        {
            // TODO: Remove before publish
#if TRUE
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
            {
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            }
#endif
        }

        private bool OnNavigating(string url)
        {
            TH.Info("OnNavigating: {0}", url);
            try
            {
                return MessagesViewHelper.ShouldOverrideRequest(Element as MessagesView, url);
            }
            catch
            { }

            return false;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            var mwv = e.OldElement as MessagesView;

            if (mwv != null)
            {
                mwv.UpdateAsync = null;
            }

            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.SetWebViewClient(new WebClient(this));
                Control.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            }

            mwv = e.NewElement as MessagesView;

            if (mwv != null)
            {
                mwv.UpdateAsync = UpdateAsync;
            }
        }

        #region InvokeScriptAsyncCore

        private Task InvokeScriptAsyncCore(string script)
        {
            var tcs = new TaskCompletionSource<string>();
            try
            {
                Control.SetWebChromeClient(new EvaluateScriptWebViewClient(Control, tcs));
                Control.EvaluateJavascript(script, new EvaluateScriptValueCallback(Control, tcs));
                return tcs.Task;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        private sealed class EvaluateScriptValueCallback : Java.Lang.Object, IValueCallback
        {
            private readonly Android.Webkit.WebView view;
            private readonly TaskCompletionSource<string> tcs;

            public EvaluateScriptValueCallback(Android.Webkit.WebView view, TaskCompletionSource<string> tcs)
            {
                this.view = view;
                this.tcs = tcs;
            }

            public void OnReceiveValue(Java.Lang.Object value)
            {
                tcs.TrySetResult(string.Empty);
                RemoveWebChromeClient(view);
            }
        }

        private sealed class EvaluateScriptWebViewClient : WebChromeClient
        {
            private readonly Android.Webkit.WebView view;
            private readonly TaskCompletionSource<string> tcs;

            public EvaluateScriptWebViewClient(Android.Webkit.WebView view, TaskCompletionSource<string> tcs)
            {
                this.view = view;
                this.tcs = tcs;
            }

            public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
            {
#if DEBUG
                TH.Info("WebView console:[{0}]{1}", consoleMessage.InvokeMessageLevel(), consoleMessage.Message());
#endif
                if (consoleMessage.InvokeMessageLevel() == ConsoleMessage.MessageLevel.Error)
                {
                    tcs.TrySetException(new Exception(consoleMessage.Message()));
                    RemoveWebChromeClient(view);
                }
                return base.OnConsoleMessage(consoleMessage);
            }
        }

        private static void RemoveWebChromeClient(Android.Webkit.WebView view)
        {
            try
            {
                if (view.IsAttachedToWindow)
                {
                    view.SetWebChromeClient(null);
                }
            }
            catch { }
        }

        #endregion InvokeScriptAsyncCore

        private bool _Loaded;

        private async Task<bool> UpdateAsync(bool reset, MessagesViewUpdateRequest[] requests, bool? hasUnread)
        {
            var mwv = Element as MessagesView;

            if (mwv != null)
            {
                if (!_Loaded)
                {
                    _Loaded = true;
                    Control.LoadDataWithBaseURL("https://kokoro.io/", MessagesViewHelper.GetHtml(), "text/html", "UTF-8", null);
                }

                var script = MessagesViewHelper.CreateScriptForRequest(mwv.Messages, reset, requests, hasUnread);
                if (!string.IsNullOrEmpty(script))
                {
                    await InvokeScriptAsyncCore(script);
                    return true;
                }
            }
            return false;
        }
    }
}