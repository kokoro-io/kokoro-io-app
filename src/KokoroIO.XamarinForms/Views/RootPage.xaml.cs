using System;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RootPage : MasterDetailPage
    {
        public RootPage(ApplicationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            Master = new RoomsPage()
            {
                BindingContext = new RoomsViewModel(viewModel)
            };
            var np = new NavigationPage(new ContentPage());
            SetupNavigationPage(np);
            Detail = np;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            IsPresented = true;
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            var vm = BindingContext as ApplicationViewModel;

            if (vm != null)
            {
                vm.SelectedRoom = null;
            }
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ApplicationViewModel.SelectedRoom))
            {
                var vm = BindingContext as ApplicationViewModel;

                var mp = vm?.SelectedRoom?.GetOrCreateMessagesPage();

                if (mp == null)
                {
                    IsPresented = true;
                }
                else if (Detail is NavigationPage np)
                {
                    if (np.CurrentPage is MessagesPage dp)
                    {
                        dp.BindingContext = mp;
                        np.Title = dp.Title;
                        IsPresented = false;
                    }
                    else
                    {
                        await np.PushAsync(new MessagesPage()
                        {
                            BindingContext = mp
                        });
                        IsPresented = false;
                    }
                }
                else
                {
                    np = new NavigationPage(new MessagesPage()
                    {
                        BindingContext = mp
                    });
                    SetupNavigationPage(np);
                    Detail = np;
                    IsPresented = false;
                }
            }
        }

        private void SetupNavigationPage(NavigationPage np)
        {
            np.Popped += NavigationPage_Popped;
        }

        private void NavigationPage_Popped(object sender, NavigationEventArgs e)
        {
            IsPresented = true;
        }
    }
}