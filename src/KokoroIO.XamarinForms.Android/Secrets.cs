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

        public const string ListenConnectionString = "Endpoint=sb://kokoro-io.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=I0TdAKFvK2KWOZ+7a0v+rLklDjuK9KgNulwzgiobbuk=";
        public const string NotificationHubName = "kokoro-io-production";
    }
}