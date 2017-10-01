namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ChannelDetailViewModel : BaseViewModel
    {
        internal ChannelDetailViewModel(ChannelViewModel channel)
        {
            Channel = channel;
        }

        public ChannelViewModel Channel { get; }

        private ObservableRangeCollection<ProfileViewModel> _Members;

        public ObservableRangeCollection<ProfileViewModel> Members
            => _Members ?? (_Members = Channel.Members);
    }
}