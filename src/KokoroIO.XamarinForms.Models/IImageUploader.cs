using System.IO;
using System.Threading.Tasks;

namespace KokoroIO.XamarinForms.Models
{
    public interface IImageUploader
    {
        string DisplayName { get; }

        string LogoImage { get; }

        bool IsAuthorized { get; }

        string AuthorizationUrl { get; }
        string CallbackUrl { get; }

        Task ReceiveCallbackAsync(string url);

        Task<UploadedImageInfo> UploadAsync(Stream data, string fileName);
    }
}