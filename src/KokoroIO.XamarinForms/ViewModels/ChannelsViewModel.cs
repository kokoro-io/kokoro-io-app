namespace KokoroIO.XamarinForms.ViewModels
{
    public class ChannelsViewModel : BaseViewModel
    {
        internal ChannelsViewModel(ApplicationViewModel application)
        {
            Application = application;
            Title = "Channels";
        }

        public ApplicationViewModel Application { get; }

        public ObservableRangeCollection<ChannelViewModel> Channels
            => Application.Channels;
    }
}