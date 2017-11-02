using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using KokoroIO.XamarinForms.ViewModels;
using Newtonsoft.Json;
using Plugin.Clipboard;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    public class MessagesView : WebView
    {
        private readonly HashSet<MessageInfo> _BindedMessages = new HashSet<MessageInfo>();

        public MessagesView()
        {
            Navigating += MessagesView_Navigating;
        }

        #region Messages

        public static readonly BindableProperty MessagesProperty
            = BindableProperty.Create(nameof(Messages), typeof(IEnumerable<MessageInfo>), typeof(MessagesView), propertyChanged: MessagesChanged);

        public IEnumerable<MessageInfo> Messages
        {
            get => (IEnumerable<MessageInfo>)GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
        }

        private static void MessagesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = (MessagesView)bindable;

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

        #endregion Messages

        #region SelectedMessage

        public static readonly BindableProperty SelectedMessageProperty
            = BindableProperty.Create(nameof(SelectedMessage), typeof(MessageInfo), typeof(MessagesView), propertyChanged: SelectedMessageChanged, defaultBindingMode: BindingMode.OneWay);

        public MessageInfo SelectedMessage
        {
            get => (MessageInfo)GetValue(SelectedMessageProperty);
            set => SetValue(SelectedMessageProperty, value);
        }

        private static async void SelectedMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = (MessagesView)bindable;

            var m = newValue as MessageInfo;
            if (m != null)
            {
                await mwv.InvokeScriptAsync($"window.showMessage({m.Id}, true)");
            }
        }

        #endregion SelectedMessage

        #region HasUnread

        public static readonly BindableProperty HasUnreadProperty
            = BindableProperty.Create(nameof(HasUnread), typeof(bool), typeof(RootPage), defaultValue: false, propertyChanged: OnHasUnreadChanged);

        public bool HasUnread
        {
            get => (bool)GetValue(HasUnreadProperty);
            set => SetValue(HasUnreadProperty, value);
        }

        private static async void OnHasUnreadChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = bindable as MessagesView;
            if (mwv != null)
            {
                await mwv.InvokeScriptAsync($"window.setHasUnread({(false.Equals(newValue) ? "false" : "true")})");
            }
        }

        #endregion HasUnread

        #region LoadOlderCommand

        public static readonly BindableProperty LoadOlderCommandProperty
            = BindableProperty.Create(nameof(LoadOlderCommand), typeof(ICommand), typeof(MessagesView));

        public ICommand LoadOlderCommand
        {
            get => (ICommand)GetValue(LoadOlderCommandProperty);
            set => SetValue(LoadOlderCommandProperty, value);
        }

        #endregion LoadOlderCommand

        #region RefreshCommand

        public static readonly BindableProperty RefreshCommandProperty
            = BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(MessagesView));

        public ICommand RefreshCommand
        {
            get => (ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        #endregion RefreshCommand

        #region NavigatingCommand

        public static readonly BindableProperty NavigatingCommandProperty
            = BindableProperty.Create(nameof(NavigatingCommand), typeof(ICommand), typeof(MessagesView));

        public ICommand NavigatingCommand
        {
            get => (ICommand)GetValue(NavigatingCommandProperty);
            set => SetValue(NavigatingCommandProperty, value);
        }

        #endregion NavigatingCommand

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
                                    js.Serialize(sw, newItems.Select(m => new MessagesViewMessage(m)));
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
                                    js.Serialize(sw, merged.OfType<MessageInfo>().Select(m => new MessagesViewMergeInfo(m)));
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
                                    js.Serialize(sw, oldItems.Select(m => m.Id).OfType<int>());
                                    sw.Write(",");
                                    js.Serialize(sw, oldItems.Select(m => m.IdempotentKey).OfType<Guid>());
                                    sw.Write(",");
                                    IEnumerable<MessageInfo> merged;
                                    if (e.OldStartingIndex >= 0)
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
                                    js.Serialize(sw, merged.OfType<MessageInfo>().Select(m => new MessagesViewMergeInfo(m)));
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
                case nameof(MessageInfo.Id):
                case nameof(MessageInfo.Profile):
                case nameof(MessageInfo.PublishedAt):
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
                    js.Serialize(sw, ms.OrderBy(m => m.Id).Select(m => new MessagesViewMessage(m)));
                    sw.Write(")");
                    _RefreshMessagesRequested = false;

                    try
                    {
                        await InvokeScriptAsync(sw.ToString());
                    }
                    catch (Exception ex)
                    {
                        ex.Error("Failed to update message");
                    }
                }
            }
        }

        #region HTML and JavaScript

        internal Func<string, Task> InvokeScriptAsyncCore;
        internal Action<string> NavigateToStringCore;


        private async Task InvokeScriptAsync(string script)
        {
            using (TH.BeginScope("Invoking script"))
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

            using (var rs = RH.GetManifestResourceStream("Messages.html"))
            using (var sr = new StreamReader(rs))
            {
                var html = sr.ReadToEnd();

                if (XDevice.Idiom == TargetIdiom.Desktop)
                {
                    html = html.Replace("<html>", "<html class=\"html-desktop\">");
                }
                else if (XDevice.Idiom == TargetIdiom.Tablet)
                {
                    html = html.Replace("<html>", "<html class=\"html-tablet\">");
                }

                NavigateToStringCore(html);
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
                        js.Serialize(sw, Messages.Select(m => new MessagesViewMessage(m)));
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

        #region Navigating

        private void MessagesView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            try
            {
                if (e.Cancel)
                {
                    return;
                }
                e.Cancel = true;

                if (HandlePrepend(e.Url)
                    || HandleAppend(e.Url)
                    || HandleVisibility(e.Url)
                    || HandleReply(e.Url)
                    || HandleCopy(e.Url)
                    || HandleDeletion(e.Url)
                    || HandleMenu(e.Url))
                {
                    return;
                }

                var c = NavigatingCommand;

                if (c?.CanExecute(e.Url) == true)
                {
                    e.Cancel = true;

                    c.Execute(e.Url);
                }
            }
            catch { }
        }

        private bool HandlePrepend(string url)
        {
            if (url == "http://kokoro.io/client/control?event=prepend")
            {
                LoadOlderCommand?.Execute(null);
                return true;
            }
            return false;
        }

        private bool HandleAppend(string url)
        {
            if (url == "http://kokoro.io/client/control?event=append")
            {
                RefreshCommand?.Execute(null);
                return true;
            }
            return false;
        }

        private bool HandleVisibility(string url)
        {
            const string IDURL = "http://kokoro.io/client/control?event=visibility&ids=";
            if (url.StartsWith(IDURL))
            {
                var messages = Messages;

                if (messages.Any())
                {
                    int? max = null;
                    int? min = null;
                    List<Guid> keys = null;

                    foreach (var id in url.Substring(IDURL.Length).Split(','))
                    {
                        if (int.TryParse(id, out var i))
                        {
                            min = Math.Min(min ?? int.MaxValue, i);
                            max = Math.Max(max ?? int.MinValue, i);
                        }
                        else if (Guid.TryParse(id, out var g))
                        {
                            (keys ?? (keys = new List<Guid>())).Add(g);
                        }
                    }

                    foreach (var m in messages)
                    {
                        m.IsShown = (min <= m.Id && m.Id <= max)
                                    || (m.Id == null
                                        && m.IdempotentKey != null
                                        && keys?.Contains(m.IdempotentKey.Value) == true);
                    }
                }

                return true;
            }
            return false;
        }

        private bool HandleReply(string url)
        {
            const string REPLY_URL = "http://kokoro.io/client/control?event=replyToMessage&id=";
            if (url.StartsWith(REPLY_URL))
            {
                if (int.TryParse(url.Substring(REPLY_URL.Length), out var id))
                {
                    var msg = Messages.FirstOrDefault(m => m.Id == id);
                    msg?.Reply();
                }
                return true;
            }
            return false;
        }

        private bool HandleCopy(string url)
        {
            const string COPY_URL = "http://kokoro.io/client/control?event=copyMessage&id=";
            if (url.StartsWith(COPY_URL))
            {
                if (int.TryParse(url.Substring(COPY_URL.Length), out var id))
                {
                    var msg = Messages.FirstOrDefault(m => m.Id == id);

                    if (msg != null)
                    {
                        CrossClipboard.Current.SetText(msg.RawContent);
                    }
                }
                return true;
            }
            return false;
        }

        private bool HandleDeletion(string url)
        {
            const string DELETEURL = "http://kokoro.io/client/control?event=deleteMessage&id=";
            if (url.StartsWith(DELETEURL))
            {
                if (int.TryParse(url.Substring(DELETEURL.Length), out var id))
                {
                    var msg = Messages.FirstOrDefault(m => m.Id == id);
                    msg?.BeginConfirmDeletion();
                }
                return true;
            }
            return false;
        }

        private bool HandleMenu(string url)
        {
            const string DELETEURL = "http://kokoro.io/client/control?event=messageMenu&id=";
            if (url.StartsWith(DELETEURL))
            {
                if (int.TryParse(url.Substring(DELETEURL.Length), out var id))
                {
                    var msg = Messages.FirstOrDefault(m => m.Id == id);
                    msg?.ShowMenu();
                }
                return true;
            }
            return false;
        }

        #endregion Navigating
    }
}