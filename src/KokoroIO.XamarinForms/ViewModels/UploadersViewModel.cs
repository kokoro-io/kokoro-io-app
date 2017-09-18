using System.Collections.Generic;
using System.Linq;
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

        private List<UploaderInfo> _Uploaders;

        public IReadOnlyList<UploaderInfo> Uploaders
            => _Uploaders ?? (_Uploaders = ApplicationViewModel.Uploaders.Select(u => new UploaderInfo(u)).ToList());
    }
}