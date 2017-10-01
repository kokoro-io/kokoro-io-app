using System;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ProfileViewModel : ObservableObject
    {
        internal ProfileViewModel(ApplicationViewModel application, Profile model)
        {
            Application = application;
            Id = model.Id;
            _Avatar = model.Avatar;
            _DisplayName = model.DisplayName;
            _ScreenName = model.ScreenName;
            _InvitedChannelCount = model.InvitedChannelCount;
            IsBot = model.Type == ProfileType.Bot;
        }

        public ApplicationViewModel Application { get; }

        public string Id { get; }

        #region Avatar

        private string _Avatar;

        public string Avatar
        {
            get => _Avatar;
            internal set => SetProperty(ref _Avatar, value);
        }

        #endregion Avatar

        #region DisplayName

        private string _DisplayName;

        public string DisplayName
        {
            get => _DisplayName;
            internal set => SetProperty(ref _DisplayName, value);
        }

        #endregion DisplayName

        #region ScreenName

        private string _ScreenName;

        public string ScreenName
        {
            get => _ScreenName;
            private set => SetProperty(ref _ScreenName, value);
        }

        #endregion ScreenName

        #region InvitedChannelCount

        private int _InvitedChannelCount;

        public int InvitedChannelCount
        {
            get => _InvitedChannelCount;
            private set => SetProperty(ref _InvitedChannelCount, value);
        }

        #endregion InvitedChannelCount

        internal void Update(Profile model)
        {
            Avatar = model.Avatar ?? _Avatar;
            DisplayName = model.DisplayName ?? _DisplayName;
            ScreenName = model.ScreenName;
            InvitedChannelCount = model.InvitedChannelCount;
        }

        public bool IsBot { get; }

        public bool IsOtherUser
            => !IsBot && Id != Application.LoginUser.Id;

        private Command _BeginDirectMessageCommand;

        public Command BeginDirectMessageCommand
            => _BeginDirectMessageCommand ?? (_BeginDirectMessageCommand = new Command(async () =>
            {
                try
                {
                    if (!IsOtherUser)
                    {
                        return;
                    }

                    var channel = await Application.PostDirectMessageChannelAsync(Id);

                    Application.SelectedChannel = channel;
                }
                catch (Exception ex)
                {
                    ex.Trace("PostDirectMessageFailed");
                }
            }));
    }
}