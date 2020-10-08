namespace KokoroIO.XamarinForms.Droid
{
    internal static partial class Secrets
    {
        // TODO: make environment variable

        public const string SenderID =
#if DEBUG
            "1001837084742"
#else
            "323911141792"
#endif
            ;
    }
}