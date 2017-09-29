using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XDevice = Xamarin.Forms.Device;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : TabbedPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;

            Children.Add(new ProfileSettingsPage()
            {
                BindingContext = viewModel.Profile
            });
            Children.Add(new NotificationSettingsPage()
            {
                BindingContext = viewModel.Notification
            });
            Children.Add(new UploaderSettingsPage()
            {
                BindingContext = viewModel.Uploader
            });
            Children.Add(new AboutPage()
            {
                // BindingContext = viewModel.Profile
            });

            if (XDevice.Idiom == TargetIdiom.Phone
                && XDevice.RuntimePlatform != XDevice.Windows)
            {
                foreach (var c in Children)
                {
                    c.Title = null;
                }
            }

            SelectedItem = Children.FirstOrDefault(c => c.GetType() == viewModel.InitialPageType) ?? SelectedItem;
            viewModel.InitialPageType = null;
        }
    }
}