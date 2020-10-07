using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    public sealed class ChannelsLabel : EntitiesLabel
    {
        public override string GetText(object item)
            => ((ChannelViewModel)item).DisplayName;
    }
}