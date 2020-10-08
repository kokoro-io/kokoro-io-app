namespace KokoroIO.XamarinForms.ViewModels
{
    public sealed class DependentLibraryViewModel
    {
        internal DependentLibraryViewModel(string displayName, string url)
        {
            DisplayName = displayName;
            Url = url;
        }

        public string DisplayName { get; }
        public string Url { get; }
    }
}