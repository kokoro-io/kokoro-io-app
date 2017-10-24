using KokoroIO.XamarinForms.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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

        #region Messages

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
            = BindableProperty.Create(nameof(SelectedMessage), typeof(MessageInfo), typeof(MessageWebView), propertyChanged: SelectedMessageChanged, defaultBindingMode: BindingMode.OneWay);

        public MessageInfo SelectedMessage
        {
            get => (MessageInfo)GetValue(SelectedMessageProperty);
            set => SetValue(SelectedMessageProperty, value);
        }

        private static async void SelectedMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = (MessageWebView)bindable;

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
            var mwv = bindable as MessageWebView;
            if (mwv != null)
            {
                await mwv.InvokeScriptAsync($"window.setHasUnread({(false.Equals(newValue) ? "false" : "true")})");
            }
        }

        #endregion HasUnread

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
                    js.Serialize(sw, ms.OrderBy(m => m.Id).Select(m => new JsonMessage(m)));
                    sw.Write(")");
                    _RefreshMessagesRequested = false;

                    try
                    {
                        await InvokeScriptAsync(sw.ToString());
                    }
                    catch (Exception ex)
                    {
                        ex.Trace("Failed to update message");
                    }
                }
            }
        }

        #region HTML and JavaScript

        internal Func<string, Task> InvokeScriptAsyncCore;
        internal Action<string> NavigateToStringCore;

        private class JsonMessage
        {
            public JsonMessage(MessageInfo m)
            {
                Id = m.Id;
                IdempotentKey = m.IdempotentKey;

                ProfileId = m.Profile.Id;
                Avatar = m.DisplayAvatar;
                ScreenName = m.Profile.ScreenName;
                DisplayName = m.Profile.DisplayName;
                IsBot = m.Profile.Type == ProfileType.Bot;
                PublishedAt = m.PublishedAt;
                IsNsfw = m.IsNsfw;
                Content = m.Content;
                IsMerged = m.IsMerged;
                EmbedContents = m.EmbedContents;
            }

            public int? Id { get; }
            public Guid? IdempotentKey { get; }
            public string ProfileId { get; }
            public string Avatar { get; }
            public string ScreenName { get; }
            public string DisplayName { get; }
            public bool IsBot { get; }
            public DateTime? PublishedAt { get; }

            public bool IsNsfw { get; }
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

            public int? Id { get; }
            public bool IsMerged { get; }
        }

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
                NavigateToStringCore(sr.ReadToEnd());
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

            const string IDURL = "http://kokoro.io/client/control?event=visibility&ids=";
            if (e.Url.StartsWith(IDURL))
            {
                e.Cancel = true;

                var messages = Messages;

                if (messages.Any())
                {
                    int? max = null;
                    int? min = null;
                    List<Guid> keys = null;

                    foreach (var id in e.Url.Substring(IDURL.Length).Split(','))
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

                return;
            }

            const string DELETEURL = "http://kokoro.io/client/control?event=deleteMessage&id=";
            if (e.Url.StartsWith(DELETEURL))
            {
                e.Cancel = true;
                if (int.TryParse(e.Url.Substring(DELETEURL.Length), out var id))
                {
                    var msg = Messages.FirstOrDefault(m => m.Id == id);
                    msg?.BeginDelete();
                }
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