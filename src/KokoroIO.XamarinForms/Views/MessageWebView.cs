using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using KokoroIO.XamarinForms.ViewModels;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public class MessageWebView : WebView
    {
        public MessageWebView()
        {
            Navigating += MessageWebView_Navigating;
        }

        public static readonly BindableProperty MessagesProperty
            = BindableProperty.Create(nameof(Messages), typeof(IEnumerable<MessageInfo>), typeof(MessageWebView), propertyChanged: MessagesChanged);

        public IEnumerable<MessageInfo> Messages
        {
            get => (IEnumerable<MessageInfo>)GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
        }

        public static readonly BindableProperty NavigatingCommandProperty
            = BindableProperty.Create(nameof(NavigatingCommand), typeof(ICommand), typeof(MessageWebView));

        public ICommand NavigatingCommand
        {
            get => (ICommand)GetValue(NavigatingCommandProperty);
            set => SetValue(NavigatingCommandProperty, value);
        }

        private static void MessagesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = (MessageWebView)bindable;

            {
                if (oldValue is INotifyCollectionChanged cc)
                {
                    cc.CollectionChanged -= mwv.Messages_CollectionChanged;
                }
            }
            {
                if (newValue is INotifyCollectionChanged cc)
                {
                    cc.CollectionChanged += mwv.Messages_CollectionChanged;
                }
            }

            mwv.RefreshMessages();
        }

        private async void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_MessagesLoaded == true)
            {
                // HTML and JavaScript has loaded.

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems?.Count > 0)
                        {
                            try
                            {
                                var js = new JsonSerializer();

                                using (var sw = new StringWriter())
                                {
                                    sw.Write("window.addMessages(");
                                    js.Serialize(sw, e.NewItems.Cast<MessageInfo>().Select(m => new JsonMessage(m)));
                                    sw.Write(",");
                                    IEnumerable<MessageInfo> merged;
                                    if (e.NewStartingIndex >= 0)
                                    {
                                        merged = new[]
                                        {
                                            Messages.ElementAtOrDefault(e.NewStartingIndex - 1),
                                            Messages.ElementAtOrDefault(e.NewStartingIndex + e.NewItems.Count)
                                        };
                                    }
                                    else
                                    {
                                        merged = Messages.Except(e.NewItems.Cast<MessageInfo>());
                                    }
                                    js.Serialize(sw, merged.OfType<MessageInfo>().Select(m => new JsonMerged(m)));
                                    sw.Write(")");

                                    await InvokeScriptAsync(sw.ToString());
                                }
                                return;
                            }
                            catch { }
                        }
                        break;
                }
            }

            RefreshMessages();
        }

        private static Stream GetManifestResourceStream(string fileName)
        {
            var t = typeof(MessageWebView);
#if WINDOWS_UWP
            return t.GetTypeInfo().Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.UWP.Resources." + fileName);
#else
#if __IOS__
            return t.Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.iOS.Resources." + fileName);
#else
            return t.Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.Droid.Resources." + fileName);
#endif
#endif
        }

        #region HTML and JavaScript

        internal Func<string, Task> InvokeScriptAsyncCore;
        private int _RetryCount;
        private bool? _MessagesLoaded;

        private class JsonMessage
        {
            public JsonMessage(MessageInfo m)
            {
                Id = m.Id;
                Avatar = m.Profile.Avatar;
                DisplayName = m.Profile.DisplayName;
                PublishedAt = m.PublishedAt;
                Content = m.Content;
                IsMerged = m.IsMerged;
            }

            public int Id { get; }
            public string Avatar { get; }
            public string DisplayName { get; }
            public DateTime PublishedAt { get; }
            public string Content { get; }
            public bool IsMerged { get; }
        }

        private class JsonMerged
        {
            public JsonMerged(MessageInfo m)
            {
                Id = m.Id;
                IsMerged = m.IsMerged;
            }

            public int Id { get; }
            public bool IsMerged { get; }
        }

        private async Task InvokeScriptAsync(string script)
        {
            if (InvokeScriptAsyncCore != null)
            {
                await InvokeScriptAsyncCore(script);
            }
            else
            {
                Eval(script);
            }
        }

        private void InitHtml()
        {
            if (Source != null && _RetryCount < 10)
            {
                return;
            }

            using (var sw = new StringWriter())
            using (var xw = XmlWriter.Create(sw, new XmlWriterSettings()
            {
                CheckCharacters = false,
                OmitXmlDeclaration = true
            }))
            {
                xw.WriteDocType("html", "", "", "");

                xw.WriteStartElement("html");
                xw.WriteStartElement("head");

                xw.WriteStartElement("meta");
                xw.WriteAttributeString("name", "viewport");
                xw.WriteAttributeString("content", "width=device-width, initial-scale=1, user-scalable=0");
                xw.WriteEndElement();

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("Messages.css"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteRaw(sr.ReadToEnd());
                }
                xw.WriteEndElement();

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("MessageBox.css"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteRaw(sr.ReadToEnd());
                }
                xw.WriteEndElement();

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("Pygments.css"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteRaw(sr.ReadToEnd());
                }
                xw.WriteEndElement();

                xw.WriteStartElement("script");
                using (var rs = GetManifestResourceStream("Messages.js"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteRaw(sr.ReadToEnd());
                }
                xw.WriteEndElement();

                xw.WriteEndElement();
                xw.WriteStartElement("body");

                xw.WriteString("");

                xw.WriteEndElement();
                xw.WriteEndElement();

                xw.Flush();

                Source = new HtmlWebViewSource()
                {
                    BaseUrl = "https://kokoro.io/",
                    Html = sw.ToString()
                };
            }
        }

        private async void RefreshMessages()
        {
            InitHtml();

            try
            {
                _MessagesLoaded = false;
                if (Messages == null)
                {
                    await InvokeScriptAsync("window.setMessages(null)");
                }
                else
                {
                    var js = new JsonSerializer();

                    using (var sw = new StringWriter())
                    {
                        sw.Write("window.setMessages(");
                        js.Serialize(sw, Messages.Select(m => new JsonMessage(m)));
                        sw.Write(")");

                        await InvokeScriptAsync(sw.ToString());
                    }
                }
                _RetryCount = 0;
                _MessagesLoaded = true;
            }
            catch
            {
                _RetryCount++;
                _MessagesLoaded = null;
                Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
                {
                    RefreshMessages();
                    return false;
                });
            }
        }

        #endregion HTML and JavaScript

        private void MessageWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            var c = NavigatingCommand;

            if (!e.Cancel && c?.CanExecute(e.Url) == true)
            {
                e.Cancel = true;
                try
                {
                    c.Execute(e.Url);
                }
                catch { }
            }
        }
    }
}