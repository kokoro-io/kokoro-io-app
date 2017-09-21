namespace KokoroIO.XamarinForms.Models
{
    internal sealed class UserSettings
    {
        public static bool PlayRingtone
        {
            get
            {
                if (App.Current.Properties.TryGetValue(nameof(PlayRingtone), out var obj)
                    && obj is bool b)
                {
                    return b;
                }
                return Xamarin.Forms.Device.Idiom == Xamarin.Forms.TargetIdiom.Desktop;
            }
            set
            {
                App.Current.Properties[nameof(PlayRingtone)] = value;
            }
        }
    }
}