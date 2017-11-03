using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    internal class MessagesViewMergeInfo
    {
        public MessagesViewMergeInfo(MessageInfo message)
        {
            Id = message.Id;
            IsMerged = message.IsMerged;
        }

        public int? Id { get; }
        public bool IsMerged { get; }
    }
}