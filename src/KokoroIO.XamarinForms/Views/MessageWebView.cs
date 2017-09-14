using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
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
            this.Navigating += MessageWebView_Navigating;
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
                    cc.CollectionChanged -= mwv.Cc_CollectionChanged;
                }
            }
            {
                if (newValue is INotifyCollectionChanged cc)
                {
                    cc.CollectionChanged += mwv.Cc_CollectionChanged;
                }
            }
            mwv.RefreshMessages();
        }

        private void Cc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

        private void RefreshMessages()
        {
            InitHtml();

            if (Messages == null)
            {
                Eval("setMessages(null)");
            }
            else
            {
                var js = new JsonSerializer();

                using (var sw = new StringWriter())
                {
                    sw.Write("setMessages(");
                    js.Serialize(sw, Messages.Select(m => new
                    {
                        m.Id,
                        m.Profile.Avatar,
                        m.Profile.DisplayName,
                        m.PublishedAt,
                        m.Content,
                        m.IsMerged
                    }));
                    sw.Write(")");

                    var script = sw.ToString();

                    Eval(script);
                }
            }
        }

        private void InitHtml()
        {
            if (Source != null)
            {
                return;
            }

            using (var sw = new StringWriter())
            using (var xw = XmlWriter.Create(sw, new XmlWriterSettings()
            {
                CheckCharacters = false
            }))
            {
                xw.WriteDocType("html", null, null, null);

                xw.WriteStartElement("html");
                xw.WriteStartElement("head");

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("Messages.css"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteString(sr.ReadToEnd());
                }
                xw.WriteEndElement();

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("MessageBox.css"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteString(sr.ReadToEnd());
                }
                xw.WriteEndElement();

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("Pygments.css"))
                using (var sr = new StreamReader(rs))
                {
                    xw.WriteString(sr.ReadToEnd());
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