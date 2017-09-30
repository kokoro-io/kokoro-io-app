using System;
using System.Collections.Generic;
using System.Text;
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

            Hub = new NotificationHub(Secrets.NotificationHubName, Secrets.ListenConnectionString,
                                        context);
            try
            {
                Hub.UnregisterAll(registrationId);
            }
            catch (Exception ex)
            {
                Log.Error(GcmBroadcastReceiver.TAG, ex.Message);
            }

            var tags = new List<string>() { };

            try
            {
                var hubRegistration = Hub.Register(registrationId, tags.ToArray());
            }
            catch (Exception ex)
            {
                Log.Error(GcmBroadcastReceiver.TAG, ex.Message);
            }
        }

        protected override void OnMessage(Context context, Intent intent)
        {
            Log.Info(GcmBroadcastReceiver.TAG, "GCM Message Received!");

            var msg = new StringBuilder();

            if (intent != null && intent.Extras != null)
            {
                foreach (var key in intent.Extras.KeySet())
                    msg.AppendLine(key + "=" + intent.Extras.Get(key).ToString());
            }

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

            CreateNotification("GCM Unregistered...", "The device has been unregistered!");
        }

        protected override bool OnRecoverableError(Context context, string errorId)
        {
            Log.Warn(GcmBroadcastReceiver.TAG, "Recoverable Error: " + errorId);

            return base.OnRecoverableError(context, errorId);
        }

        protected override void OnError(Context context, string errorId)
        {
            Log.Error(GcmBroadcastReceiver.TAG, "GCM Error: " + errorId);
        }
    }
}