using KokoroIO.XamarinForms.Helpers;

namespace KokoroIO.XamarinForms.ViewModels
{
    public abstract class MessageBlockBase : ObservableObject
    {
        internal MessageBlockBase(MessageInfo message)
        {
            Message = message;
        }

        internal MessageInfo Message { get; }
    }
}