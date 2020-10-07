using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class MembersLabel : EntitiesLabel
    {
        public override string GetText(object item)
            => "@" + ((ProfileViewModel)item).ScreenName;
    }
}