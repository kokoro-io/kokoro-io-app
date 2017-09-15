//using Android.Media;
//using KokoroIO.XamarinForms.Droid;
//using Xamarin.Forms;

//[assembly: Dependency(typeof(AudioService))]

//namespace KokoroIO.XamarinForms.Droid
//{
//    public sealed class AudioService : IAudioService
//    {
//        private MediaPlayer _NotificationPlayer;

//        public void PlayNotification()
//        {
//            if (_NotificationPlayer == null)
//            {
//                _NotificationPlayer = MediaPlayer.Create(Android.App.Application.Context, Resource.Raw.ring);
//                _NotificationPlayer.Start();
//            }
//            else
//            {
//                _NotificationPlayer.Reset();
//                _NotificationPlayer.Start();
//            }
//        }
//    }
//}