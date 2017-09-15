using AVFoundation;
using KokoroIO.XamarinForms.iOS;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioService))]

namespace KokoroIO.XamarinForms.iOS
{
    public sealed class AudioService : IAudioService
    {
        private AVAudioPlayer _NotificationPlayer;

        public void PlayNotification()
        {
            if (_NotificationPlayer == null)
            {
                _NotificationPlayer = new AVAudioPlayer("Resources/ring.mp3", "mp3", out _);
                _NotificationPlayer.NumberOfLoops = 1;
                _NotificationPlayer.Play();
            }
            else
            {
                _NotificationPlayer.Stop();
                _NotificationPlayer.Play();
            }
        }
    }
}