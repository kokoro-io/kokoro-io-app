using System;
using System.Linq;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ChannelListViewModel : BaseViewModel
    {
        internal ChannelListViewModel(ApplicationViewModel application)
        {
            Application = application;
            Title = "Explore channels";
        }

        public ApplicationViewModel Application { get; }

        #region All

        private ObservableRangeCollection<ChannelViewModel> _All;

        private ObservableRangeCollection<ChannelViewModel> All
        {
            get
            {
                if (_All == null)
                {
                    _All = new ObservableRangeCollection<ChannelViewModel>();
                    _All.CollectionChanged += All_CollectionChanged;
                    BeginLoadAll();
                }
                return _All;
            }
        }

        private async void BeginLoadAll()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;

                All.ReplaceRange((await Application.GetChannelsAsync(false))
                                    .OrderBy(c => c.ChannelName, StringComparer.OrdinalIgnoreCase));
            }
            catch { }
            finally
            {
                IsBusy = false;
            }
        }

        private void All_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_ShowInvitations)
            {
                UpdateChannels();
            }
        }

        #endregion All

        #region Invitations

        private ObservableRangeCollection<ChannelViewModel> _Invitations;

        private ObservableRangeCollection<ChannelViewModel> Invitations
        {
            get
            {
                if (_Invitations == null)
                {
                    _Invitations = new ObservableRangeCollection<ChannelViewModel>();
                    _Invitations.CollectionChanged += Invitations_CollectionChanged;
                    BeginLoadInvitations();
                }
                return _Invitations;
            }
        }

        private async void BeginLoadInvitations()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;

                Invitations.ReplaceRange(
                        (await Application.GetMembershipsAsync(false, Authority.Invited))
                            .Select(m => Application.GetOrCreateChannelViewModel(m.Channel))
                            .OrderBy(c => c.ChannelName, StringComparer.OrdinalIgnoreCase));
            }
            catch { }
            finally
            {
                IsBusy = false;
            }
        }

        private void Invitations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_ShowInvitations)
            {
                UpdateChannels();
            }
        }

        #endregion Invitations

        #region Query

        private string _Query;

        public string Query
        {
            get => _Query;
            set => SetProperty(ref _Query, value, onChanged: UpdateChannels);
        }

        #endregion Query

        #region ShowInvitations

        private bool _ShowInvitations;

        public bool ShowInvitations
        {
            get => _ShowInvitations;
            set => SetProperty(ref _ShowInvitations, value, onChanged: UpdateChannels);
        }

        #endregion ShowInvitations

        #region Channels

        private ObservableRangeCollection<ChannelViewModel> _Channels;

        public ObservableRangeCollection<ChannelViewModel> Channels
        {
            get
            {
                if (_Channels == null)
                {
                    _Channels = new ObservableRangeCollection<ChannelViewModel>();
                    UpdateChannels();
                }
                return _Channels;
            }
        }

        private void UpdateChannels()
        {
            if (_Channels == null)
            {
                return;
            }

            var src = _ShowInvitations ? Invitations : All;

            if (string.IsNullOrWhiteSpace(_Query))
            {
                _Channels.ReplaceRange(src);
            }
            else
            {
                _Channels.ReplaceRange(src.Where(c => c.ChannelName.Contains(_Query) || c.Description.Contains(_Query)));
            }
        }

        #endregion Channels

        private Command _ShowInvitationsCommand;

        public Command ShowInvitationsCommand
            => _ShowInvitationsCommand ?? (_ShowInvitationsCommand = new Command(p =>
            {
                ShowInvitations = true.Equals(p) || bool.TrueString.Equals(p as string);
            }));
    }
}