using KokoroIO.XamarinForms.Helpers;

namespace KokoroIO.XamarinForms.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        private bool isBusy = false;

        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        /// <summary>
        /// Private backing field to hold the title
        /// </summary>
        private string title = string.Empty;

        /// <summary>
        /// Public property to set and get the title of the item
        /// </summary>
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
    }
}