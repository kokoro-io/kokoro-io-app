using System;
using System.Threading.Tasks;
using Android.Webkit;
using KokoroIO.XamarinForms.Droid;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(MessageWebView), typeof(MessageWebViewRenderer))]

namespace KokoroIO.XamarinForms.Droid
{
    public class MessageWebViewRenderer : WebViewRenderer
    {
        static MessageWebViewRenderer()
        {
#if DEBUG
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
            {
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            }
#endif
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
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
                view.SetWebChromeClient(null);
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
                    view.SetWebChromeClient(null);
                }
                return base.OnConsoleMessage(consoleMessage);
            }
        }

        #endregion InvokeScriptAsyncCore

        private void NavigateToStringCore(string html)
        {
            Control.LoadDataWithBaseURL("https://kokoro.io/", html, "text/html", "UTF-8", null);
        }
    }
}