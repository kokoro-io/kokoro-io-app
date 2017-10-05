using System.IO;
using Android.Media;
using KokoroIO.XamarinForms.Droid.Services;
using KokoroIO.XamarinForms.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioService))]

namespace KokoroIO.XamarinForms.Droid.Services
{
    public sealed class AudioService : IAudioService
    {
        private string _FilePath;

        public void PlayNotification()
        {
            if (_FilePath == null)
            {
                _FilePath = Path.GetTempFileName();
                _FilePath = Path.ChangeExtension(_FilePath, ".mp3");
                using (var fs = new FileStream(_FilePath, FileMode.Create))
                using (var rs = GetType().Assembly.GetManifestResourceStream("KokoroIO.XamarinForms.Droid.Resources.ring.mp3"))
                {
                    rs.CopyTo(fs);
                }
            }
            var c = MainActivity.GetCurrentContext();

            if (c != null)
            {
                var rm = RingtoneManager.GetRingtone(c, Android.Net.Uri.FromFile(new Java.IO.File(_FilePath)));
                rm.Play();
            }
            else
            {
                var mp = new MediaPlayer();
                mp.SetDataSource(_FilePath);
                mp.Prepare();
                mp.Start();
            }
        }
    }
}