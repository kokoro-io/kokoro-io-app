﻿using System;
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
        private readonly Action<string> _Callback;

        private bool _HasNavigated;

        public UploaderAuthorizationPage(ApplicationViewModel application, IImageUploader uploader, Action<string> callback)
        {
            InitializeComponent();

            _Application = application;
            _Uploader = uploader;
            _Callback = callback;
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

                    await _Application.BeginSelectImage(_Uploader, _Callback);
                }
                catch
                {
                    // TODO: show alert in appvm

                    while (Navigation.ModalStack.Count > 0)
                    {
                        await Navigation.PopModalAsync();
                    }
                }
            }
        }
    }
}