using System;
using System.IO;
using System.Reflection;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.UWP.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioService))]

namespace KokoroIO.XamarinForms.UWP.Services
{
    public sealed class AudioService : IAudioService
    {
        private MediaPlayer _NotificationPlayer;

        public void PlayNotification()
        {
            if (_NotificationPlayer == null)
            {
                _NotificationPlayer = new MediaPlayer()
                {
                    Source = Windows.Media.Core.MediaSource.CreateFromStream(
                                GetType().GetTypeInfo().Assembly
                                    .GetManifestResourceStream("KokoroIO.XamarinForms.UWP.Resources.ring.mp3")
                                    .AsRandomAccessStream(),
                                "audio/mp3")
                };
            }
            else
            {
                _NotificationPlayer.Pause();
                _NotificationPlayer.PlaybackSession.Position = TimeSpan.Zero;
            }

            _NotificationPlayer.Play();
        }
    }
}