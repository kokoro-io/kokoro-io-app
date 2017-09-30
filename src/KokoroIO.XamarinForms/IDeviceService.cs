using System.Threading.Tasks;

namespace KokoroIO.XamarinForms
{
    public interface IDeviceService
    {
        string MachineName { get; }

        DeviceKind Kind { get; }

        byte[] GetIdentifier();

        Task<string> GetPlatformNotificationServiceHandleAsync();

        void UnregisterPlatformNotificationService();
    }
}