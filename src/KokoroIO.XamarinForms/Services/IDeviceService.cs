namespace KokoroIO.XamarinForms.Services
{
    public interface IDeviceService
    {
        string MachineName { get; }

        DeviceKind Kind { get; }

        byte[] GetIdentifier();

        float GetDisplayScale();
    }
}