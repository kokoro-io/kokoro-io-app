using System;
using System.IO;
using System.Linq;
using KokoroIO.XamarinForms.ViewModels;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KokoroIO.XamarinForms.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            LaunchCore(e);
        }

        private void LaunchCore(IActivatedEventArgs e, string tmp = null)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // rootFrame.Visibility = Visibility.Collapsed;

                rootFrame.NavigationFailed += OnNavigationFailed;

                Xamarin.Forms.Forms.Init(e);

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage));
            }
            // Ensure the current window is active
            Window.Current.Activate();

            if (tmp != null)
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    ApplicationViewModel.OpenFile(() => new FileStream(tmp, FileMode.Open));
                });
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        //protected override async void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        //{
        //    string tmp = null;
        //    if (args.ShareOperation.Data.Contains(StandardDataFormats.StorageItems))
        //    {
        //        var sis = await args.ShareOperation.Data.GetStorageItemsAsync();
        //        var si = sis.OfType<StorageFile>().FirstOrDefault();

        //        if (si != null)
        //        {
        //            tmp = Path.GetTempFileName();
        //            var tf = await StorageFile.GetFileFromPathAsync(tmp);
        //            await si.CopyAsync(await tf.GetParentAsync(), Path.GetFileName(tmp), NameCollisionOption.ReplaceExisting);
        //        }
        //    }
        //    else if (args.ShareOperation.Data.Contains(StandardDataFormats.Bitmap))
        //    {
        //        var sis = await args.ShareOperation.Data.GetBitmapAsync();

        //        using (var ras = await sis.OpenReadAsync())
        //        using (var ss = ras.AsStreamForRead())
        //        using (var fs = new FileStream(tmp, FileMode.Create))
        //        {
        //            await ss.CopyToAsync(fs);
        //        }
        //    }

        //    if (tmp == null)
        //    {
        //        Window.Current.Close();
        //    }
        //    else
        //    {
        //        LaunchCore(args, tmp);
        //    }
        //}

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            var p = args.Files.OfType<StorageFile>().FirstOrDefault();

            if (p == null)
            {
                Window.Current.Close();
            }
            else
            {
                var tmp = Path.GetTempFileName();

                var tf = await StorageFile.GetFileFromPathAsync(tmp);

                await p.CopyAsync(await tf.GetParentAsync(), Path.GetFileName(tmp), NameCollisionOption.ReplaceExisting);

                LaunchCore(args, tmp);
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            var avm = XamarinForms.App.Current?.MainPage?.BindingContext as ApplicationViewModel;

            if (avm != null)
            {
                if (args is ToastNotificationActivatedEventArgs tne)
                {
                    var qp = tne.Argument.ParseQueryString();

                    if (qp.TryGetValue("channelId", out var cid))
                    {
                        avm.SelectedChannelId = cid;

                        if (avm.SelectedChannel?.Id == cid
                            && qp.TryGetValue("messageId", out var mid)
                            && int.TryParse(mid, out var id))
                        {
                            avm.SelectedChannel.GetOrCreateMessagesPage().SelectedMessageId = id;
                        }


                        return;
                    }
                }
            }

            base.OnActivated(args);
        }
    }
}