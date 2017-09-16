using Shipwreck.KokoroIO;

namespace KokoroIO.XamarinForms
{
    public interface IDeviceService
    {
        string MachineName { get; }

        DeviceKind Kind { get; }

        byte[] GetIdentifier();
    }
}