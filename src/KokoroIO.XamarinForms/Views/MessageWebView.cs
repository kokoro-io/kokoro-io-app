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
using Shipwreck.KokoroIO;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    public class MessageWebView : WebView
    {
        private readonly HashSet<MessageInfo> _BindedMessages = new HashSet<MessageInfo>();

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

        #region LoadOlderCommand

        public static readonly BindableProperty LoadOlderCommandProperty
            = BindableProperty.Create(nameof(LoadOlderCommand), typeof(ICommand), typeof(MessageWebView));

        public ICommand LoadOlderCommand
        {
            get => (ICommand)GetValue(LoadOlderCommandProperty);
            set => SetValue(LoadOlderCommandProperty, value);
        }

        #endregion LoadOlderCommand

        #region RefreshCommand

        public static readonly BindableProperty RefreshCommandProperty
            = BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(MessageWebView));

        public ICommand RefreshCommand
        {
            get => (ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        #endregion RefreshCommand

        #region NavigatingCommand

        public static readonly BindableProperty NavigatingCommandProperty
            = BindableProperty.Create(nameof(NavigatingCommand), typeof(ICommand), typeof(MessageWebView));

        public ICommand NavigatingCommand
        {
            get => (ICommand)GetValue(NavigatingCommandProperty);
            set => SetValue(NavigatingCommandProperty, value);
        }

        #endregion NavigatingCommand

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
            if (_InitialMessagesLoaded)
            {
                // HTML and JavaScript has loaded.

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems?.Count > 0)
                        {
                            var newItems = e.NewItems.Cast<MessageInfo>();
                            foreach (var item in newItems)
                            {
                                item.PropertyChanged -= Item_PropertyChanged;
                                item.PropertyChanged += Item_PropertyChanged;
                                _BindedMessages.Add(item);
                            }

                            try
                            {
                                var js = new JsonSerializer();

                                using (var sw = new StringWriter())
                                {
                                    sw.Write("window.addMessages(");
                                    js.Serialize(sw, newItems.Select(m => new JsonMessage(m)));
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
                                        merged = Messages.Except(newItems);
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

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems?.Count > 0)
                        {
                            var oldItems = e.OldItems.Cast<MessageInfo>();
                            foreach (var item in oldItems)
                            {
                                item.PropertyChanged -= Item_PropertyChanged;
                                _BindedMessages.Remove(item);
                            }

                            try
                            {
                                var js = new JsonSerializer();

                                using (var sw = new StringWriter())
                                {
                                    sw.Write("window.removeMessages(");
                                    js.Serialize(sw, oldItems.Select(m => m.Id));
                                    sw.Write(",");
                                    IEnumerable<MessageInfo> merged;
                                    if (e.NewStartingIndex >= 0)
                                    {
                                        merged = new[]
                                        {
                                            Messages.ElementAtOrDefault(e.OldStartingIndex - 1),
                                            Messages.ElementAtOrDefault(e.OldStartingIndex )
                                        };
                                    }
                                    else
                                    {
                                        merged = Messages.Except(oldItems);
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

        private HashSet<MessageInfo> _Updating;

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MessageInfo.Content):
                case nameof(MessageInfo.EmbedContents):
                    if (_InitialMessagesLoaded == true)
                    {
                        var up = _Updating ?? (_Updating = new HashSet<MessageInfo>());
                        if (up.Add((MessageInfo)sender) && up.Count == 1)
                        {
                            XDevice.BeginInvokeOnMainThread(BeginUpdateMessages);
                        }
                    }
                    break;
            }
        }

        private async void BeginUpdateMessages()
        {
            var ms = _Updating?.ToArray();
            _Updating?.Clear();

            if (ms?.Length > 0)
            {
                var js = new JsonSerializer();

                using (var sw = new StringWriter())
                {
                    sw.Write("window.addMessages(");
                    js.Serialize(sw, ms.OrderBy(m => m.Id).Select(m => new JsonMessage(m)));
                    sw.Write(")");
                    _RefreshMessagesRequested = false;

                    await InvokeScriptAsync(sw.ToString());
                }
            }
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
        internal Action<string> NavigateToStringCore;

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
                EmbedContents = m.EmbedContents;
            }

            public int Id { get; }
            public string Avatar { get; }
            public string DisplayName { get; }
            public DateTime PublishedAt { get; }
            public string Content { get; }
            public bool IsMerged { get; }
            public IList<EmbedContent> EmbedContents { get; }
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

        #region InitHtmlTask

        private Task _InitHtmlTask;

        private Task InitHtmlTask
            => _InitHtmlTask ?? (_InitHtmlTask = InitHtmlCore());

        private async Task InitHtmlCore()
        {
            while (NavigateToStringCore == null)
            {
                await Task.Delay(100);
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

                xw.WriteStartElement("base");
                xw.WriteAttributeString("href", "https://kokoro.io/");
                xw.WriteEndElement();

                xw.WriteStartElement("meta");
                xw.WriteAttributeString("name", "viewport");
                xw.WriteAttributeString("content", "width=device-width, initial-scale=1, user-scalable=0");
                xw.WriteEndElement();

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("Application.css"))
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

                xw.WriteStartElement("style");
                using (var rs = GetManifestResourceStream("Messages.css"))
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

                NavigateToStringCore(sw.ToString());
            }
        }

        #endregion InitHtmlTask

        private bool _InitialMessagesLoaded;
        private bool _RefreshMessagesRequested;

        private async void RefreshMessages()
        {
            if (_RefreshMessagesRequested)
            {
                return;
            }

            _RefreshMessagesRequested = true;
            await InitHtmlTask;
            try
            {
                if (Messages == null)
                {
                    await InvokeScriptAsync("window.setMessages(null)");
                }
                else
                {
                    foreach (var item in _BindedMessages)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                    _BindedMessages.Clear();
                    foreach (var item in Messages)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        item.PropertyChanged += Item_PropertyChanged;
                        _BindedMessages.Add(item);
                    }

                    var js = new JsonSerializer();

                    using (var sw = new StringWriter())
                    {
                        sw.Write("window.setMessages(");
                        js.Serialize(sw, Messages.Select(m => new JsonMessage(m)));
                        sw.Write(")");
                        _RefreshMessagesRequested = false;

                        await InvokeScriptAsync(sw.ToString());
                    }
                }
                _InitialMessagesLoaded = true;
            }
            catch
            {
                XDevice.StartTimer(TimeSpan.FromMilliseconds(100), () =>
                {
                    RefreshMessages();
                    return false;
                });
            }
        }

        #endregion HTML and JavaScript

        private void MessageWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }

            if (e.Url == "http://kokoro.io/client/control?event=prepend")
            {
                e.Cancel = true;
                LoadOlderCommand?.Execute(null);
                return;
            }
            if (e.Url == "http://kokoro.io/client/control?event=append")
            {
                e.Cancel = true;
                RefreshCommand?.Execute(null);
                return;
            }

            var c = NavigatingCommand;

            if (c?.CanExecute(e.Url) == true)
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