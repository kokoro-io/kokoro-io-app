using System;
using System.IO;

namespace KokoroIO.XamarinForms.ViewModels
{
    internal sealed class UploadParameter
    {
        public UploadParameter(Action<string> onCompleted, Action<string> onFaulted = null, Stream data = null, bool useCamera = false)
        {
            OnCompleted = onCompleted;
            OnFaulted = onFaulted;
            Data = data;
            UseCamera = useCamera;
        }

        public Action<string> OnCompleted { get; }

        public Action<string> OnFaulted { get; }

        public Stream Data { get; }

        public bool UseCamera { get; }
    }
}