using System;
using System.Collections.Generic;
using KokoroIO.XamarinForms.Models;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploadersViewModel : BaseViewModel
    {
        internal UploadersViewModel(ApplicationViewModel application, Action<string> callback)
        {
            Application = application;
            Callback = callback;
        }

        internal ApplicationViewModel Application { get; }

        internal Action<string> Callback { get; }

        public IReadOnlyList<IImageUploader> Uploaders => ApplicationViewModel.Uploaders;
    }
}