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

            MessagingCenter.Subscribe<MessageInfo>(this, "ConfirmMessageDeletion", async mi =>
            {
                if (mi?.IsDeleted != false
                || (BindingContext as MessagesViewModel)?.Messages.Contains(mi) != true)
                {
                    return;
                }
                if (await DisplayAlert(mi.Page.Channel.DisplayName, "Are you sure to delete the message?.", "Delete", "Cancel"))
                {
                    mi.BeginDelete();
                }
            });

            MessagingCenter.Subscribe<MessageInfo>(this, "MessageDeletionFailed", mi =>
            {
                if ((BindingContext as MessagesViewModel)?.Messages.Contains(mi) == true)
                {
                    DisplayAlert(mi.Page.Channel.DisplayName, "Failed to delete the message", "OK");
                }
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
    }
}