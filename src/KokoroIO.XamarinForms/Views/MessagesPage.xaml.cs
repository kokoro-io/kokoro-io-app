using System;
using System.IO;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MessagesPage : ContentPage
    {
        public MessagesPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<MessagesViewModel>(this, "LoadMessageFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to load messages", "OK");
            });
            MessagingCenter.Subscribe<MessagesViewModel>(this, "PostMessageFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to post a message", "OK");
            });

            MessagingCenter.Subscribe<MessagesViewModel>(this, "UploadImageFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to upload an image", "OK");
            });
            MessagingCenter.Subscribe<MessagesViewModel>(this, "TakePhotoFailed", lvm =>
            {
                DisplayAlert("kokoro.io", "Failed to take a photo", "OK");
            });
        }

        private void ExpandableEditor_FilePasted(object sender, EventArgs<Stream> e)
        {
            var vm = BindingContext as MessagesViewModel;

            if (vm == null)
            {
                e.Data.Dispose();
                return;
            }

            vm.BeginUploadImage(e.Data);
        }

        private void ExpandableEditor_Unfocused(object sender, FocusEventArgs e)
        {
            var vm = BindingContext as MessagesViewModel;

            if (vm?.CandicateClicked > DateTime.Now.AddSeconds(-0.5))
            {
                e.VisualElement.Focus();
            }
        }

        //private void PopupWebView_BindingContextChanged(object sender, EventArgs e)
        //{
        //    var mvm = BindingContext as MessagesViewModel;

        //    if (mvm.PopupUrl != null)
        //    {
        //        var r = 9.0 / 16;

        //        var w = Math.Min(480, Width);
        //        popupWebView.WidthRequest = w;
        //        popupWebView.HeightRequest = r * w;

        //        popupWebView.Source = new UrlWebViewSource()
        //        {
        //            Url = mvm.PopupUrl
        //        };
        //        webViewPopup.IsVisible = true;
        //    }
        //    else
        //    {
        //        webViewPopup.IsVisible = false;
        //        popupWebView.Source = new UrlWebViewSource()
        //        {
        //            Url = "about:blank"
        //        };
        //    }
        //}

        private async void RefreshViewToolbarItem_Clicked(object sender, EventArgs e)
        {
            try
            {
                var nav = Navigation;

                var np = new MessagesPage()
                {
                    BindingContext = BindingContext
                };
                nav.InsertPageBefore(np, this);
                await nav.PopAsync();

                np.editor?.InvalidateMeasure();
            }
            catch { }
        }
    }
}