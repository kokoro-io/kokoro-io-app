using System;
using Xamarin.Forms;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ProfileViewModel
    {
        internal ProfileViewModel(ApplicationViewModel application, Profile model)
        {
            Application = application;
            Id = model.Id;
            Avatar = model.Avatar;
            DisplayName = model.DisplayName;
            ScreenName = model.ScreenName;
            IsBot = model.Type == ProfileType.Bot;
        }

        internal ProfileViewModel(ApplicationViewModel application, string id, string avatar, string displayName, string screenName, bool isBot)
        {
            Application = application;
            Id = id;
            Avatar = avatar;
            DisplayName = displayName;
            ScreenName = screenName;
            IsBot = isBot;
        }

        public ApplicationViewModel Application { get; }

        public string Id { get; }
        public string Avatar { get; }
        public string DisplayName { get; }
        public string ScreenName { get; }
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