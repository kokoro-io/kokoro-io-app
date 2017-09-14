using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.Views
{
    public class MessageWebView : WebView
    {
        public static readonly BindableProperty MessagesProperty
            = BindableProperty.Create(nameof(Messages), typeof(IEnumerable<MessageInfo>), typeof(MessageWebView), propertyChanged: MessagesChanged);

        public IEnumerable<MessageInfo> Messages
        {
            get => (IEnumerable<MessageInfo>)GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
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
            return t.Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.Android.Resources." + fileName);
#endif
#endif
        }

        private void RefreshMessages()
        {
            string xml;
            var replacements = new Dictionary<string, string>();
            using (var sw = new StringWriter())
            using (var xw = XmlWriter.Create(sw))
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

                xw.WriteString("");

                xw.WriteEndElement();
                xw.WriteStartElement("body");

                var ms = Messages;

                if (ms != null)
                {
                    foreach (var m in ms)
                    {
                        xw.WriteStartElement("div");
                        xw.WriteAttributeString("class", m.IsMerged ? "talk not-continued" : "talk continued");
                        xw.WriteAttributeString("data-message-id", m.Id.ToString("D"));
                        {
                            xw.WriteStartElement("div");
                            xw.WriteAttributeString("class", "avatar");
                            {
                                xw.WriteStartElement("a");
                                xw.WriteAttributeString("class", "img-rounded");

                                xw.WriteStartElement("img");
                                xw.WriteAttributeString("src", m.Profile.Avatar);

                                xw.WriteEndElement();
                                xw.WriteEndElement();
                            }
                            xw.WriteEndElement();

                            xw.WriteStartElement("div");
                            xw.WriteAttributeString("class", "message");
                            {
                                xw.WriteStartElement("div");
                                xw.WriteAttributeString("class", "speaker");
                                {
                                    xw.WriteStartElement("a");
                                    xw.WriteString(m.Profile.DisplayName);
                                    xw.WriteEndElement();

                                    xw.WriteStartElement("small");
                                    xw.WriteAttributeString("class", "timeleft text-muted");
                                    xw.WriteString(m.PublishedAt.ToString("MM/dd HH:mm"));
                                    xw.WriteEndElement();
                                }
                                xw.WriteEndElement();

                                xw.WriteStartElement("div");
                                xw.WriteAttributeString("class", "filtered_text");
                                {
                                    var cm = $"<!--{m.Id}-->";
                                    xw.WriteRaw(cm);

                                    replacements[cm] = m.Content;
                                }
                                xw.WriteEndElement();
                            }
                            xw.WriteEndElement();
                        }
                        xw.WriteEndElement();
                    }
                }

                xw.WriteEndElement();
                xw.WriteEndElement();

                xw.Flush();

                xml = sw.ToString();
            }

            foreach (var kv in replacements)
            {
                xml = xml.Replace(kv.Key, kv.Value);
            }

            Source = new HtmlWebViewSource()
            {
                BaseUrl = "https://kokoro.io/",
                Html = xml
            };
        }
    }
}