namespace KokoroIO.XamarinForms.Services
{
    public interface IDeviceService
    {
        string MachineName { get; }

        DeviceKind Kind { get; }

        float GetDisplayScale();
    }
}