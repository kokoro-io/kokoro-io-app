namespace KokoroIO.XamarinForms.ViewModels
{
    public class MenuViewModel : BaseViewModel
    {
        internal MenuViewModel(ApplicationViewModel application)
        {
            Application = application;
            Title = "Menu";
        }

        public ApplicationViewModel Application { get; }

        public ObservableRangeCollection<ChannelViewModel> Channels
            => Application.Channels;
    }
}