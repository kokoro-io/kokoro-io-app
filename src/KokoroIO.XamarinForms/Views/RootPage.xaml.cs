using System;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XDevice = Xamarin.Forms.Device;

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

            if (viewModel.SelectedRoom == null)
            {
                var np = new NavigationPage(new WelcomePage());
                SetupNavigationPage(np);
                Detail = np;

                IsPresented = true;
            }
            else
            {
                var np = new NavigationPage(new MessagesPage()
                {
                    BindingContext = viewModel.SelectedRoom.GetOrCreateMessagesPage()
                });
                SetupNavigationPage(np);
                Detail = np;

                IsPresented = false;
            }
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            MasterBehavior = XDevice.Idiom == TargetIdiom.Phone ? MasterBehavior.Popover : MasterBehavior.Split;

            SetBinding(HasNotificationProperty, new Binding(nameof(viewModel.HasNotificationInMenu)));
        }

        public static readonly BindableProperty HasNotificationProperty
            = BindableProperty.Create(nameof(HasNotification), typeof(bool), typeof(RootPage), defaultValue: false);

        public bool HasNotification
        {
            get => (bool)GetValue(HasNotificationProperty);
            set => SetValue(HasNotificationProperty, value);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var vm = BindingContext as ApplicationViewModel;

            vm?.BeginProcessPendingFile();
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
                    SetIsPresented(true);
                }
                else if (Detail is NavigationPage np)
                {
                    if (np.CurrentPage is MessagesPage dp)
                    {
                        dp.BindingContext = mp;
                        np.Title = dp.Title;
                        SetIsPresented(false);
                    }
                    else
                    {
                        var det = new MessagesPage()
                        {
                            BindingContext = mp
                        };

                        await np.PushAsync(det);

                        while (det.Navigation.NavigationStack.Count > 1)
                        {
                            det.Navigation.RemovePage(det.Navigation.NavigationStack.FirstOrDefault(p => p != det));
                        }

                        SetIsPresented(false);
                    }
                }
                else
                {
                    var det = new MessagesPage()
                    {
                        BindingContext = mp
                    };
                    np = new NavigationPage(det);
                    SetupNavigationPage(np);
                    Detail = np;
                    SetIsPresented(false);
                }
            }
        }

        private void SetupNavigationPage(NavigationPage np)
        {
            np.Popped += NavigationPage_Popped;
        }

        private void NavigationPage_Popped(object sender, NavigationEventArgs e)
        {
            SetIsPresented(true);
        }

        private void SetIsPresented(bool value)
        {
            if (value)
            {
                IsPresented = true;
            }
            else if (MasterBehavior != MasterBehavior.Split
                    && MasterBehavior != MasterBehavior.SplitOnLandscape
                    && MasterBehavior != MasterBehavior.SplitOnPortrait)
            {
                IsPresented = false;
            }
        }
    }
}