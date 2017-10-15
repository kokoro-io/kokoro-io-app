using System.Linq;

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

                All.ReplaceRange(await Application.GetChannelsAsync(false));
            }
            catch { }
            finally
            {
                IsBusy = false;
            }
        }

        private void All_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => UpdateChannels();

        #endregion All

        #region Query

        private string _Query;

        public string Query
        {
            get => _Query;
            set => SetProperty(ref _Query, value, onChanged: UpdateChannels);
        }

        #endregion Query

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

            if (string.IsNullOrWhiteSpace(_Query))
            {
                _Channels.ReplaceRange(All);
            }
            else
            {
                _Channels.ReplaceRange(All.Where(c => c.ChannelName.Contains(_Query) || c.Description.Contains(_Query)));
            }
        }

        #endregion Channels
    }
}