using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ProfileViewModel
    {
        internal ProfileViewModel(Profile model)
        {
            Id = model.Id;
            Avatar = model.Avatar;
            DisplayName = model.DisplayName;
            ScreenName = model.ScreenName;
        }

        internal ProfileViewModel(string id, string avatar, string displayName, string screenName)
        {
            Id = id;
            Avatar = avatar;
            DisplayName = displayName;
            ScreenName = screenName;
        }

        public string Id { get; }
        public string Avatar { get; }
        public string DisplayName { get; }
        public string ScreenName { get; }
    }
}