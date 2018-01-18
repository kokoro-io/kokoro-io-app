using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Webkit;
using KokoroIO.XamarinForms.Droid.Renderers;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(MessagesView), typeof(MessagesViewRenderer))]

namespace KokoroIO.XamarinForms.Droid.Renderers
{
    public class MessagesViewRenderer : WebViewRenderer
    {
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

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            var mwv = e.OldElement as MessagesView;

            if (mwv != null)
            {
                mwv.InvokeScriptAsyncCore = null;
                mwv.NavigateToStringCore = null;
            }

            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            }

            mwv = e.NewElement as MessagesView;

            if (mwv != null)
            {
                mwv.InvokeScriptAsyncCore = InvokeScriptAsyncCore;
                mwv.NavigateToStringCore = NavigateToStringCore;
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

        private void NavigateToStringCore(string html)
        {
            Control.LoadDataWithBaseURL("https://kokoro.io/", html, "text/html", "UTF-8", null);
        }
    }
}