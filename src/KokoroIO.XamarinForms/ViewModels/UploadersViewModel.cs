using System.Collections.Generic;
using KokoroIO.XamarinForms.Models;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploadersViewModel : BaseViewModel
    {
        internal UploadersViewModel(ApplicationViewModel application, UploadParameter parameter)
        {
            Application = application;
            Parameter = parameter;
        }

        internal ApplicationViewModel Application { get; }

        internal UploadParameter Parameter { get; }

        public IReadOnlyList<IImageUploader> Uploaders => ApplicationViewModel.Uploaders;
    }
}