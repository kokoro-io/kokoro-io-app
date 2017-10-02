using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using Gcm.Client;
using WindowsAzure.Messaging;

namespace KokoroIO.XamarinForms.Droid
{
    [Service]
    public class PushHandlerService : GcmServiceBase
    {
        private static TaskCompletionSource<string> _RegistrationTaskSource
            = new TaskCompletionSource<string>();

        internal static Task<string> RegistrationTask => _RegistrationTaskSource.Task;

        public static string RegistrationID { get; private set; }
        private NotificationHub Hub { get; set; }

        public PushHandlerService() : base(Secrets.SenderID)
        {
            Log.Info(GcmBroadcastReceiver.TAG, "PushHandlerService() constructor");
        }

        protected override void OnRegistered(Context context, string registrationId)
        {
            Log.Verbose(GcmBroadcastReceiver.TAG, "GCM Registered: " + registrationId);
            RegistrationID = registrationId;
            _RegistrationTaskSource.TrySetResult(registrationId);
        }

        protected override void OnMessage(Context context, Intent intent)
        {
            var msg = new StringBuilder();

            if (intent != null && intent.Extras != null)
            {
                foreach (var key in intent.Extras.KeySet())
                {
                    msg.AppendLine(key + "=" + intent.Extras.Get(key).ToString());
                }
            }

            Log.Info(GcmBroadcastReceiver.TAG, "GCM Message Received!: " + msg);



            string messageText = intent.Extras.GetString("message");
            if (!string.IsNullOrEmpty(messageText))
            {
                CreateNotification("New hub message!", messageText);
            }
            else
            {
                CreateNotification("Unknown message details", msg.ToString());
            }
        }

        private void CreateNotification(string title, string desc)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new Notification.Builder(this)
                .SetSmallIcon(Resource.Drawable.kokoro_white)
                .SetContentTitle(title)
                .SetContentText(desc)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetPriority((int)NotificationPriority.High);

            var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
            notificationManager.Notify(0, notificationBuilder.Build());
        }

        protected override void OnUnRegistered(Context context, string registrationId)
        {
            Log.Verbose(GcmBroadcastReceiver.TAG, "GCM Unregistered: " + registrationId);

            if (_RegistrationTaskSource.Task.IsCompleted)
            {
                _RegistrationTaskSource = new TaskCompletionSource<string>();
            }
        }

        protected override bool OnRecoverableError(Context context, string errorId)
        {
            if (RegistrationID == null)
            {
                _RegistrationTaskSource.TrySetException(new Exception(errorId));
            }

            Log.Warn(GcmBroadcastReceiver.TAG, "Recoverable Error: " + errorId);

            return base.OnRecoverableError(context, errorId);
        }

        protected override void OnError(Context context, string errorId)
        {
            if (RegistrationID == null)
            {
                _RegistrationTaskSource.TrySetException(new Exception(errorId));
            }

            Log.Error(GcmBroadcastReceiver.TAG, "GCM Error: " + errorId);
        }

        internal static Task<string> RegisterAsync()
        {
            if (RegistrationID == null)
            {
                var ma = MainActivity.GetCurrentActivity();

                if (ma != null)
                {
                    try
                    {
                        // Check to ensure everything's set up right
                        GcmClient.CheckDevice(ma);
                        GcmClient.CheckManifest(ma);

                        // Register for push notifications
                        System.Diagnostics.Debug.WriteLine("Registering...");
                        GcmClient.Register(ma, Secrets.SenderID);

                        return RegistrationTask;
                    }
                    catch (Java.Net.MalformedURLException)
                    {
                        ma.CreateAndShowDialog("There was an error creating the client. Verify the URL.", "Error");
                    }
                    catch (Exception e)
                    {
                        ma.CreateAndShowDialog(e.Message, "Error");
                    }
                }

                return Task.FromResult<string>(null);
            }

            return RegistrationTask;
        }

        public static void Unregister()
        {
            if (RegistrationID != null)
            {
                var ma = MainActivity.GetCurrentActivity();

                try
                {
                    RegistrationID = null;
                    if (_RegistrationTaskSource.Task.IsCompleted)
                    {
                        _RegistrationTaskSource = new TaskCompletionSource<string>();
                    }
                    GcmClient.UnRegister(ma);
                }
                catch (Exception e)
                {
                    ma.CreateAndShowDialog(e.Message, "Error");
                }
            }
        }
    }
}