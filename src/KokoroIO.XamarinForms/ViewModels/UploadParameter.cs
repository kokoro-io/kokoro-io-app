using System;
using System.IO;

namespace KokoroIO.XamarinForms.ViewModels
{
    internal sealed class UploadParameter
    {
        public UploadParameter(Action<string> onCompleted, Action<string> onFaulted = null, Stream data = null)
        {
            OnCompleted = onCompleted;
            OnFaulted = onFaulted;
            Data = data;
        }

        public Action<string> OnCompleted { get; }

        public Action<string> OnFaulted { get; }

        public Stream Data { get; }
    }
}