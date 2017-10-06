using System;
using System.IO;

namespace KokoroIO.XamarinForms.ViewModels
{
    internal sealed class UploadParameter
    {
        public UploadParameter(Action<string> onCompleted, Action<string> onFaulted = null, Action onUploading = null, Stream data = null, bool useCamera = false)
        {
            OnCompleted = onCompleted;
            OnFaulted = onFaulted;
            OnUploading = onUploading;
            Data = data;
            UseCamera = useCamera;
        }

        public Action OnUploading { get; set; }

        public Action<string> OnCompleted { get; }

        public Action<string> OnFaulted { get; }

        public Stream Data { get; }

        public bool UseCamera { get; }
    }
}