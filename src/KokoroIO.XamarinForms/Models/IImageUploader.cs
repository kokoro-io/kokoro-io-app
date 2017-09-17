using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace KokoroIO.XamarinForms.Models
{
    public interface IImageUploader
    {
        string DisplayName { get; }

        bool IsAuthorized { get; }

        string AuthorizationUrl { get; }
        string CallbackUrl { get; }

        Task ReceiveCallbackAsync(string url);

        Task<string> UploadAsync(Stream data, string fileName);
    }

}