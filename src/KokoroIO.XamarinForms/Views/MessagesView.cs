using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    public class MessagesView : WebView
    {
        private readonly HashSet<MessageInfo> _BindedMessages = new HashSet<MessageInfo>();
        private bool _HasMessages;

        internal Func<bool, MessagesViewUpdateRequest[], bool?, Task<bool>> UpdateAsync;

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
            = BindableProperty.Create(nameof(HasUnread), typeof(bool), typeof(MessagesView), defaultValue: false, propertyChanged: OnHasUnreadChanged);

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
                case nameof(MessageInfo.HtmlContent):
                case nameof(MessageInfo.EmbedContents):
                    EnqueueAdd(new[] { (MessageInfo)sender });
                    break;

                case nameof(MessageInfo.IsMerged):
                    EnqueueIsMergedChanged((MessageInfo)sender);
                    break;
            }
        }

        #region View updating queue

        private readonly List<MessagesViewUpdateRequest> _Requests = new List<MessagesViewUpdateRequest>();
        private bool _IsReset;
        private bool? _HasUnread;
        private bool _IsRequestProcessing;
        private bool _HtmlLoaded;

        private int _UpdateRetryCount;

        private void EnqueueReset()
        {
            lock (_Requests)
            {
                if (_IsReset)
                {
                    return;
                }

                var rq = _Requests.LastOrDefault(r => r.Type == MessagesViewUpdateType.Show);
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
                _Requests.RemoveAll(r => r.Type != MessagesViewUpdateType.Show && es.Any(e => e == r.Message));
                _Requests.AddRange(es.Distinct().Select(e => new MessagesViewUpdateRequest(MessagesViewUpdateType.Added, e)));
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
                _Requests.AddRange(es.Distinct().Select(e => new MessagesViewUpdateRequest(MessagesViewUpdateType.Removed, e)));
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

                if (_Requests.Any(r => r.Type == MessagesViewUpdateType.Added && r.Message == message))
                {
                    return;
                }
                _Requests.Add(new MessagesViewUpdateRequest(MessagesViewUpdateType.IsMergedChanged, message));
                BeginProcessUpdates();
            }
        }

        private void EnqueueShow(MessageInfo message)
        {
            lock (_Requests)
            {
                _Requests.RemoveAll(r => r.Type == MessagesViewUpdateType.Show);
                _Requests.Add(new MessagesViewUpdateRequest(MessagesViewUpdateType.Show, message));
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
            MessagesViewUpdateRequest[] requests;
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

                if (!_HasMessages && Messages?.Any() != true)
                {
                    return;
                }

                if (!(_IsRequestProcessing = (reset || requests.Any() || hasUnread != null)))
                {
                    _UpdateRetryCount = 0;
                    return;
                }
            }

            try
            {
                var t = UpdateAsync?.Invoke(reset, requests, hasUnread);
                if (t != null && await t.ConfigureAwait(false))
                {
                    _HasMessages = Messages?.Any() == true;
                }
                _UpdateRetryCount = 0;
            }
            catch (Exception ex)
            {
                if (_HtmlLoaded)
                {
                    ex.Error("FailedToUpdateMessagesView");
                }

                if (_UpdateRetryCount++ > 100)
                {
                    return;
                }

                var sm = requests.LastOrDefault(r => r.Type == MessagesViewUpdateType.Show)?.Message;
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

        private void EnqueueFailedRequests(bool reset, MessagesViewUpdateRequest[] requests, MessageInfo showing)
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
                            if (m.Type == MessagesViewUpdateType.Show)
                            {
                                continue;
                            }

                            var cur = _Requests.FirstOrDefault(r => r.Type != MessagesViewUpdateType.Show && r.Message == m.Message);
                            switch (m.Type)
                            {
                                case MessagesViewUpdateType.Added:
                                    if (cur?.Type != MessagesViewUpdateType.Removed)
                                    {
                                        _Requests.Add(m);
                                        valid = true;
                                    }
                                    break;

                                case MessagesViewUpdateType.Removed:
                                case MessagesViewUpdateType.IsMergedChanged:
                                    if (cur == null)
                                    {
                                        _Requests.Add(m);
                                        valid = true;
                                    }
                                    break;
                            }
                        }
                    }

                    if (showing != null && _Requests.Any(r => r.Type == MessagesViewUpdateType.Show))
                    {
                        _Requests.Add(new MessagesViewUpdateRequest(MessagesViewUpdateType.Show, showing));
                        valid = true;
                    }
                    if (valid)
                    {
                        XDevice.BeginInvokeOnMainThread(() =>
                        {
                            XDevice.StartTimer(TimeSpan.FromMilliseconds(250), () =>
                            {
                                BeginProcessUpdates();
                                return false;
                            });
                        });
                    }
                }
            }
        }

        #endregion View updating queue
    }
}