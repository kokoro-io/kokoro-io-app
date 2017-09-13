using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class ApplicationViewModel
    {
        internal ApplicationViewModel(Client client)
        {
            Client = client;
        }

        internal Client Client { get; }
    }
}