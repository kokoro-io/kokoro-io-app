using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ChannelDetailViewModel : BaseViewModel
    {
        internal ChannelDetailViewModel(ChannelViewModel channel)
        {
            Channel = channel;
        }

        public ChannelViewModel Channel { get; }

        public ApplicationViewModel Application => Channel.Application;

        private ObservableRangeCollection<ProfileViewModel> _Members;

        public ObservableRangeCollection<ProfileViewModel> Members
            => _Members ?? (_Members = Channel.Members);

        #region LeaveCommand

        private Command _LeaveCommand;

        public Command LeaveCommand
            => _LeaveCommand ?? (_LeaveCommand = new Command(async () =>
           {
               if (IsBusy || Channel.MembershipId == null)
               {
                   return;
               }

               IsBusy = true;

               try
               {
                   await Application.DeleteMembershipAsync(Channel.MembershipId);
                   Channel.ClearMembership();
               }
               finally
               {
                   IsBusy = false;
               }
           }));

        #endregion MyRegion
    }
}