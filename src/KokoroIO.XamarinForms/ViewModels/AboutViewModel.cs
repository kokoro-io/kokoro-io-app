using System.Collections.ObjectModel;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class AboutViewModel : BaseViewModel
    {
        private ReadOnlyCollection<DependentLibraryViewModel> _Dependencies;

        public ReadOnlyCollection<DependentLibraryViewModel> Dependencies
        {
            get
            {
                if (_Dependencies == null)
                {
                    _Dependencies = new ReadOnlyCollection<DependentLibraryViewModel>(new[]
                    {
                        new  DependentLibraryViewModel("XLabs", "https://github.com/XLabs/Xamarin-Forms-Labs"),
                        new  DependentLibraryViewModel("Material icons", "https://material.io/icons/"),
                        new  DependentLibraryViewModel("Bootstrap 3.3.7", "https://getbootstrap.com/docs/3.3/"),
                        new  DependentLibraryViewModel("portable-exif-lib", "https://github.com/ravensorb/portable-exif-lib"),
                    });
                }
                return _Dependencies;
            }
        }
    }
}