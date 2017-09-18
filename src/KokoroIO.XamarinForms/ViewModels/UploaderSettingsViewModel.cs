using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploaderSettingsViewModel : BaseViewModel
    {
        public UploaderSettingsViewModel(SettingsViewModel settings)
        {
            Title = "Uploader";
            Settings = settings;
        }

        internal SettingsViewModel Settings { get; }
        internal ApplicationViewModel Application => Settings.Application;

        private List<UploaderInfo> _Uploaders;

        public IReadOnlyList<UploaderInfo> Uploaders
            => _Uploaders ?? (_Uploaders = ApplicationViewModel.Uploaders.Select(u => new UploaderInfo(u)).ToList());
    }
}