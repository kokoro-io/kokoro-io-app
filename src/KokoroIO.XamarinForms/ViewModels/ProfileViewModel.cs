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
        }

        internal ProfileViewModel(string id, string avatar, string displayName)
        {
            Id = id;
            Avatar = avatar;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string Avatar { get; }
        public string DisplayName { get; }
    }
}