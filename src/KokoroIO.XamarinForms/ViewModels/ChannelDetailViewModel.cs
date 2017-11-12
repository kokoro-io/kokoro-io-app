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

        #region JoinCommand

        private Command _JoinCommand;

        public Command JoinCommand
            => _JoinCommand ?? (_JoinCommand = new Command(async () =>
            {
                if (IsBusy
                    || (Channel.MembershipId != null && Channel.Authority != Authority.Invited))
                {
                    return;
                }

                IsBusy = true;

                try
                {
                    await Application.PostMembershipAsync(Channel.Id);
                    OnPropertyChanged(nameof(CanJoin));
                    if (_Members?.Count > 0 && !_Members.Contains(Application.LoginUser))
                    {
                        _Members.Add(Application.LoginUser);
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }));

        public bool CanJoin
            => !Channel.IsArchived
            && (Channel.Authority == null || Channel.Authority == Authority.Invited);

        #endregion JoinCommand

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
                   OnPropertyChanged(nameof(CanJoin));
               }
               finally
               {
                   IsBusy = false;
               }
           }));

        #endregion LeaveCommand
    }
}