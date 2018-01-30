using KokoroIO.XamarinForms.ViewModels;

namespace KokoroIO.XamarinForms.Views
{
    internal sealed class MessagesViewUpdateRequest
    {
        public MessagesViewUpdateRequest(MessagesViewUpdateType type, MessageInfo message)
        {
            Type = type;
            Message = message;
        }

        public MessagesViewUpdateType Type { get; }
        public MessageInfo Message { get; }
    }
}