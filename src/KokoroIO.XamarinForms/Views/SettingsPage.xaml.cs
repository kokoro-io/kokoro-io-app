using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : TabbedPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;

            Children.Add(new UploaderSettingsPage()
            {
                BindingContext = viewModel.Uploader
            });
            Children.Add(new ProfileSettingsPage()
            {
                BindingContext = viewModel.Profile
            });
        }
    }
}