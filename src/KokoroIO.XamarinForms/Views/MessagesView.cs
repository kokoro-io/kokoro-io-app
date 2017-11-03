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

            mwv?.EnqueueReset();
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

        private static void SelectedMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = (MessagesView)bindable;

            var m = newValue as MessageInfo;
            if (m != null)
            {
                mwv?.EnqueueShow(m);
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

        private static void OnHasUnreadChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mwv = bindable as MessagesView;
            mwv?.EnqueueHasUnread(!false.Equals(newValue));
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

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

                        EnqueueAdd(newItems);
                        return;
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

                        EnqueueRemove(oldItems);
                        return;
                    }
                    break;
            }

            EnqueueReset();
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MessageInfo.Id):
                case nameof(MessageInfo.Profile):
                case nameof(MessageInfo.PublishedAt):
                case nameof(MessageInfo.Content):
                case nameof(MessageInfo.EmbedContents):
                    EnqueueAdd(new[] { (MessageInfo)sender });
                    break;

                case nameof(MessageInfo.IsMerged):
                    EnqueueIsMergedChanged((MessageInfo)sender);
                    break;
            }
        }

        #region HTML and JavaScript

        #region View updating queue

        private enum UpdateType
        {
            // Inbound events.
            // 1. set_Messages.
            // 2. Messages#CollectionChanged.
            // 3. MessageInfo#PropertyChanged.
            // 4. Show message
            // 5. set_HasUnread
            Added,

            Removed,
            IsMergedChanged,
            Show,
        }

        private sealed class UpdateRequest
        {
            public UpdateRequest(UpdateType type, MessageInfo message)
            {
                Type = type;
                Message = message;
            }

            public UpdateType Type { get; }
            public MessageInfo Message { get; }
        }

        private readonly List<UpdateRequest> _Requests = new List<UpdateRequest>();
        private bool _IsReset;
        private bool? _HasUnread;
        private bool _IsRequestProcessing;
        private bool _HtmlLoaded;

        private void EnqueueReset()
        {
            lock (_Requests)
            {
                if (_IsReset)
                {
                    return;
                }

                var rq = _Requests.LastOrDefault(r => r.Type == UpdateType.Show);
                _Requests.Clear();
                _IsReset = true;

                if (rq?.Message != null
                    && Messages?.Contains(rq.Message) == true)
                {
                    _Requests.Add(rq);
                }
                BeginProcessUpdates();
            }
        }

        private void EnqueueAdd(IEnumerable<MessageInfo> items)
        {
            var es = items as IReadOnlyCollection<MessageInfo> ?? items.ToList();
            lock (_Requests)
            {
                if (_IsReset)
                {
                    return;
                }
                _Requests.RemoveAll(r => r.Type != UpdateType.Show && es.Any(e => e == r.Message));
                _Requests.AddRange(es.Distinct().Select(e => new UpdateRequest(UpdateType.Added, e)));
                BeginProcessUpdates();
            }
        }

        private void EnqueueRemove(IEnumerable<MessageInfo> items)
        {
            var es = items as IReadOnlyCollection<MessageInfo> ?? items.ToList();
            lock (_Requests)
            {
                _Requests.RemoveAll(r => es.Any(e => e == r.Message));
                if (_IsReset)
                {
                    return;
                }
                _Requests.AddRange(es.Distinct().Select(e => new UpdateRequest(UpdateType.Removed, e)));
                BeginProcessUpdates();
            }
        }

        private void EnqueueIsMergedChanged(MessageInfo message)
        {
            lock (_Requests)
            {
                if (_IsReset)
                {
                    return;
                }

                if (_Requests.Any(r => r.Type == UpdateType.Added && r.Message == message))
                {
                    return;
                }
                _Requests.Add(new UpdateRequest(UpdateType.IsMergedChanged, message));
                BeginProcessUpdates();
            }
        }

        private void EnqueueShow(MessageInfo message)
        {
            lock (_Requests)
            {
                _Requests.RemoveAll(r => r.Type == UpdateType.Show);
                _Requests.Add(new UpdateRequest(UpdateType.Show, message));
                BeginProcessUpdates();
            }
        }

        private void EnqueueHasUnread(bool hasUnread)
        {
            lock (_Requests)
            {
                var hadValue = _HasUnread != null;
                _HasUnread = hasUnread;
                if (!hadValue)
                {
                    BeginProcessUpdates();
                }
            }
        }

        private void BeginProcessUpdates()
            => XDevice.BeginInvokeOnMainThread(ProcessUpdates);

        private async void ProcessUpdates()
        {
            bool reset;
            UpdateRequest[] requests;
            bool? hasUnread;

            lock (_Requests)
            {
                if (_IsRequestProcessing)
                {
                    return;
                }
                reset = _IsReset;
                requests = _Requests.ToArray();
                hasUnread = _HasUnread;
                _IsReset = false;
                _Requests.Clear();
                _HasUnread = null;

                if (!(_IsRequestProcessing = reset || requests.Any()) || _HasUnread != null)
                {
                    return;
                }
            }

            var sm = requests.LastOrDefault(r => r.Type == UpdateType.Show)?.Message;
            try
            {
                using (var sw = new StringWriter())
                {
                    if (reset)
                    {
                        sw.Write("window.setMessages(");
                        if (Messages == null)
                        {
                            sw.Write("null");
                            TH.Info("Clearing messages of MessagesView");
                        }
                        else
                        {
                            new JsonSerializer().Serialize(
                                sw, Messages.Select(m => new MessagesViewMessage(m))
                                            .OrderBy(m => m.PublishedAt ?? DateTime.MaxValue));
                            TH.Info("Setting {0} messages to MessagesView", Messages.Count());
                        }

                        sw.WriteLine(");");
                    }
                    else
                    {
                        List<MessageInfo> added = null, removed = null, merged = null;
                        foreach (var g in requests.GroupBy(r => r.Message))
                        {
                            if (g.Any(r => r.Type == UpdateType.Added))
                            {
                                (added ?? (added = new List<MessageInfo>())).Add(g.Key);
                            }
                            else if (g.Any(r => r.Type == UpdateType.Removed))
                            {
                                (removed ?? (removed = new List<MessageInfo>())).Add(g.Key);
                            }
                            else if (g.Any(r => r.Type == UpdateType.IsMergedChanged))
                            {
                                (merged ?? (merged = new List<MessageInfo>())).Add(g.Key);
                            }
                        }

                        var js = new JsonSerializer();

                        if (removed != null)
                        {
                            sw.Write("window.removeMessages(");
                            js.Serialize(sw, removed.Select(m => m.Id).OfType<int>());
                            sw.Write(",");
                            js.Serialize(sw, removed.Select(m => m.IdempotentKey).OfType<Guid>());
                            sw.Write(",");
                            if (merged != null && added == null)
                            {
                                js.Serialize(sw, merged.Select(m => new MessagesViewMergeInfo(m)));
                            }
                            else
                            {
                                sw.Write("null");
                            }
                            sw.WriteLine(");");
                            TH.Info("Removing {0} messages of MessagesView", removed.Count);
                        }
                        if (added != null || (merged != null && removed == null))
                        {
                            sw.Write("window.addMessages(");
                            if (added != null)
                            {
                                js.Serialize(
                                    sw,
                                    added.Select(m => new MessagesViewMessage(m))
                                            .OrderBy(m => m.PublishedAt ?? DateTime.MaxValue));
                                TH.Info("Adding {0} messages to MessagesView", added.Count);
                            }
                            else
                            {
                                sw.Write("[]");
                            }
                            sw.Write(",");
                            if (merged != null)
                            {
                                js.Serialize(sw, merged.Select(m => new MessagesViewMergeInfo(m)));
                            }
                            else
                            {
                                sw.Write("[]");
                            }
                            sw.WriteLine(");");
                        }
                    }
                    if (sm?.Id > 0)
                    {
                        sw.WriteLine("window.showMessage({0}, true);", sm.Id);
                        TH.Info("Showing message#{0} in MessagesView", sm.Id);
                    }
                    if (_HasUnread != null)
                    {
                        sw.WriteLine("window.setHasUnread({0});", hasUnread == true ? "true" : "false");
                    }

                    var script = sw.ToString();
                    if (!string.IsNullOrEmpty(script))
                    {
                        await InvokeScriptAsync(script).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_HtmlLoaded)
                {
                    ex.Error("FailedToUpdateMessagesView");
                }

                EnqueueFailedRequests(reset, requests, sm);
            }
            finally
            {
                lock (_Requests)
                {
                    _IsRequestProcessing = false;
                    if (_IsReset || _Requests.Any())
                    {
                        BeginProcessUpdates();
                    }
                }
            }
        }

        private void EnqueueFailedRequests(bool reset, UpdateRequest[] requests, MessageInfo showing)
        {
            if (reset)
            {
                EnqueueReset();
            }
            else
            {
                lock (_Requests)
                {
                    var valid = false;
                    if (!_IsReset)
                    {
                        foreach (var m in requests)
                        {
                            if (m.Type == UpdateType.Show)
                            {
                                continue;
                            }

                            var cur = _Requests.FirstOrDefault(r => r.Type != UpdateType.Show && r.Message == m.Message);
                            switch (m.Type)
                            {
                                case UpdateType.Added:
                                    if (cur?.Type != UpdateType.Removed)
                                    {
                                        _Requests.Add(m);
                                        valid = true;
                                    }
                                    break;

                                case UpdateType.Removed:
                                case UpdateType.IsMergedChanged:
                                    if (cur == null)
                                    {
                                        _Requests.Add(m);
                                        valid = true;
                                    }
                                    break;
                            }
                        }
                    }

                    if (showing != null && _Requests.Any(r => r.Type == UpdateType.Show))
                    {
                        _Requests.Add(new UpdateRequest(UpdateType.Show, showing));
                        valid = true;
                    }
                    if (valid)
                    {
                        BeginProcessUpdates();
                    }
                }
            }
        }

        #endregion View updating queue

        internal Func<string, Task> InvokeScriptAsyncCore;
        internal Action<string> NavigateToStringCore;

        private bool _HtmlInitialized;

        private async Task InvokeScriptAsync(string script)
        {
            await InitHtmlAsync();

            if (InvokeScriptAsyncCore != null)
            {
                await InvokeScriptAsyncCore(script).ConfigureAwait(false);
            }
            else
            {
                Eval(script);
            }
        }

        private async Task InitHtmlAsync()
        {
            if (!_HtmlInitialized)
            {
                while (NavigateToStringCore == null)
                {
                    await Task.Delay(100).ConfigureAwait(false);
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
                _HtmlInitialized = true;
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

                if (HandleLoaded(e.Url)
                    || HandlePrepend(e.Url)
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

        private bool HandleLoaded(string url)
        {
            const string PREFIX = "http://kokoro.io/client/control?event=loaded";
            if (url == PREFIX)
            {
                _HtmlLoaded = true;
                return true;
            }
            return false;
        }

        private bool HandlePrepend(string url)
        {
            const string PREFIX = "http://kokoro.io/client/control?event=prepend&count=";
            if (url.StartsWith(PREFIX))
            {
                if (int.TryParse(url.Substring(PREFIX.Length), out var c)
                    && c == 0)
                {
                    return true;
                }
                LoadOlderCommand?.Execute(null);
                return true;
            }
            return false;
        }

        private bool HandleAppend(string url)
        {
            const string PREFIX = "http://kokoro.io/client/control?event=append&count=";
            if (url.StartsWith(PREFIX))
            {
                if (int.TryParse(url.Substring(PREFIX.Length), out var c)
                    && c == 0)
                {
                    return true;
                }
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