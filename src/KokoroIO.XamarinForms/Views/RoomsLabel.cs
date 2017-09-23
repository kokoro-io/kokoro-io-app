using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class RoomsLabel : EntitiesLabel
    {
        internal override string GetText(object item)
            => ((RoomViewModel)item).DisplayName;
    }
}