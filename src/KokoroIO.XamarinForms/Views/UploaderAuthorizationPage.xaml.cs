using System;
using KokoroIO.XamarinForms.Models;
using KokoroIO.XamarinForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace KokoroIO.XamarinForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UploaderAuthorizationPage : ContentPage
    {
        private readonly ApplicationViewModel _Application;
        private readonly IImageUploader _Uploader;

        //private readonly UploadParameter _Parameter;
        private readonly Action _Completed;

        private readonly Action<string> _Faulted;

        private bool _HasNavigated;

        private UploaderAuthorizationPage(ApplicationViewModel application, IImageUploader uploader, Action completed, Action<string> faulted)
        {
            InitializeComponent();

            _Application = application;
            _Uploader = uploader;
            _Completed = completed;
            _Faulted = faulted;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (!_HasNavigated)
            {
                _HasNavigated = true;
                webView.Source = new UrlWebViewSource()
                {
                    Url = _Uploader.AuthorizationUrl
                };
            }
        }

        private async void WebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            if (!e.Cancel && e.Url.StartsWith(_Uploader.CallbackUrl))
            {
                e.Cancel = true;

                try
                {
                    await _Uploader.ReceiveCallbackAsync(e.Url);

                    while (Navigation.ModalStack.Count > 0)
                    {
                        await Navigation.PopModalAsync();
                    }

                    _Completed();

                    //await _Application.BeginSelectImage(_Uploader, _Parameter);
                }
                catch (Exception ex)
                {
                    ex.Trace("Image Uploader Authentication failed");

                    _Faulted?.Invoke(ex.Message);
                }
            }
        }

        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            _Faulted?.Invoke(null);
        }

        internal static void BeginAuthorize(ApplicationViewModel application, IImageUploader uploader, Action completed, Action<string> faulted)
            => App.Current.MainPage.Navigation.PushModalAsync(new UploaderAuthorizationPage(application, uploader, completed, faulted)).GetHashCode();

        internal static void BeginAuthorize(ApplicationViewModel application, IImageUploader uploader, UploadParameter parameter)
            => BeginAuthorize(
                application,
                uploader,
                () => application.BeginSelectImage(uploader, parameter).GetHashCode(),
                async e =>
                {
                    parameter.OnFaulted?.Invoke(e);

                    var nav = App.Current.MainPage.Navigation;

                    while (nav.ModalStack.Count > 0)
                    {
                        await nav.PopModalAsync();
                    }
                });

        internal static void BeginAuthorize(ApplicationViewModel application, IImageUploader uploader, Action completed)
        {
            var nav = App.Current.MainPage.Navigation;
            var pc = nav.ModalStack.Count;
            nav.PushModalAsync(new UploaderAuthorizationPage(application, uploader,
                  async () =>
                  {
                      while (pc < nav.ModalStack.Count)
                      {
                          await nav.PopModalAsync();
                      }
                      completed();
                  },
                  async (e) =>
                  {
                      while (pc < nav.ModalStack.Count)
                      {
                          await nav.PopModalAsync();
                      }
                      completed();
                  })).GetHashCode();
        }
    }
}