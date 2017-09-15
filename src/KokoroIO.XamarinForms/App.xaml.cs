﻿using KokoroIO.XamarinForms.Views;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Distribute;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace KokoroIO.XamarinForms
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            SetMainPage();
        }

        protected override void OnStart()
        {
            base.OnStart();

            MobileCenter.Start("android=2bf93410-91e9-48a0-ac2a-b7cd2b2b62c1;", typeof(Analytics), typeof(Crashes), typeof(Distribute));

        }

        public static void SetMainPage()
        {
            Current.MainPage = new LoginPage();
        }
    }
}