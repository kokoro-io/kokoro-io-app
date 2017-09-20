using System;
using System.Collections.ObjectModel;

namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class AboutViewModel : BaseViewModel
    {
        public class DependentLibrary
        {
            public DependentLibrary(string displayName, string url)
            {
                DisplayName = displayName;
                Url = url;
            }

            public string DisplayName { get; }
            public string Url { get; }
        }

        private ReadOnlyCollection<DependentLibrary> _Dependencies;

        public ReadOnlyCollection<DependentLibrary> Dependencies
        {
            get
            {
                if (_Dependencies == null)
                {
                    _Dependencies = new ReadOnlyCollection<DependentLibrary>(new[]
                    {
                        new  DependentLibrary("XLabs", "https://github.com/XLabs/Xamarin-Forms-Labs"),
                        new  DependentLibrary("portable-exif-lib", "https://github.com/ravensorb/portable-exif-lib"),
                        new  DependentLibrary("Bootstrap 3.3.7", "https://getbootstrap.com/docs/3.3/")
                    });

                }
                return _Dependencies;
            }
        }

    }
}