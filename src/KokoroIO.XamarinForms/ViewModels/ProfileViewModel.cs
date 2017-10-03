using System;
using System.Linq;
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
            Avatars = model.Avatars;
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
            internal set => SetProperty(ref _Avatar, value, onChanged: () =>
            {
                OnPropertyChanged(nameof(DisplayAvatar));
                OnPropertyChanged(nameof(Avatar120px));
            });
        }

        internal Avatar[] Avatars { get; set; }

        public string DisplayAvatar
            => GetAvatarForSize(40);

        public string Avatar120px
            => GetAvatarForSize(120);

        private string GetAvatarForSize(int size)
        {
            if (Avatars != null)
            {
                var s = Application.DisplayScale * size;
                return Avatars.OrderBy(a => a.Size).Where(a => a.Size >= s).FirstOrDefault()?.Url
                        ?? Avatars.OrderByDescending(a => a.Size).FirstOrDefault()?.Url
                        ?? _Avatar;
            }

            return _Avatar;
        }

        #endregion Avatar

        #region DisplayName

        private string _DisplayName;

        public string DisplayName
        {
            get => _DisplayName;
            private set => SetProperty(ref _DisplayName, value, onChanged: () => OnPropertyChanged(nameof(FullDisplayName)));
        }

        #endregion DisplayName

        #region ScreenName

        private string _ScreenName;

        public string ScreenName
        {
            get => _ScreenName;
            private set => SetProperty(ref _ScreenName, value, onChanged: () => OnPropertyChanged(nameof(FullDisplayName)));
        }

        #endregion ScreenName

        public string FullDisplayName
            => $"{DisplayName} (@{ScreenName})";

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
            Avatars = model.Avatars ?? Avatars;
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