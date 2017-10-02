using System;
using System.Linq;
using System.Security.Cryptography;

namespace KokoroIO.XamarinForms
{
    internal static class DeviceServiceExtensions
    {
        private const string TokenName = nameof(KokoroIO) + "." + nameof(XamarinForms);

        public static string GetDeviceIdentifierString(this IDeviceService ds)
        {
            var pnsTask = ds.GetPlatformNotificationServiceHandleAsync();

            var hash = TokenName.Select(ch => (byte)ch).Concat(ds.GetIdentifier()).ToArray();

            hash = SHA256.Create().ComputeHash(hash);

            return Convert.ToBase64String(hash);
        }
    }
}