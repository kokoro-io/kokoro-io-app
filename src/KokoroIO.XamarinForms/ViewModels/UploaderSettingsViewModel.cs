using System.Collections.Generic;
using System.Linq;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class UploaderSettingsViewModel : BaseViewModel, IUploaderInfoHost
    {
        public UploaderSettingsViewModel(SettingsViewModel settings)
        {
            Title = "Uploader";
            Settings = settings;

            Uploaders = ApplicationViewModel.Uploaders.Select(u => new UploaderInfo(u, this)).ToList();

            var su = ApplicationViewModel.GetSelectedUploader();

            SelectedUploader = Uploaders.FirstOrDefault(u => u.Uploader == su);
        }

        internal SettingsViewModel Settings { get; }
        public ApplicationViewModel Application => Settings.Application;

        public IReadOnlyList<UploaderInfo> Uploaders { get; }

        private UploaderInfo _SelectedUploader;

        public UploaderInfo SelectedUploader
        {
            get => _SelectedUploader;
            set => SetProperty(ref _SelectedUploader, value, onChanged: async () =>
            {
                foreach (var up in Uploaders)
                {
                    up.IsSelected = _SelectedUploader == up;
                }
                ApplicationViewModel.SetSelectedUploader(_SelectedUploader?.Uploader);

                await App.Current.SavePropertiesAsync();
            });
        }
    }
}