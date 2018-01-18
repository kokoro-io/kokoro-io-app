using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using Gcm.Client;
using KokoroIO.XamarinForms.ViewModels;
using Newtonsoft.Json.Linq;

namespace KokoroIO.XamarinForms.Droid
{
    [Service]
    public class PushHandlerService : GcmServiceBase
    {
        private static TaskCompletionSource<string> _RegistrationTaskSource
            = new TaskCompletionSource<string>();

        internal static Task<string> RegistrationTask => _RegistrationTaskSource.Task;

        private static WeakReference<PushHandlerService> _Current;

        internal static PushHandlerService Current
        {
            get => _Current != null && _Current.TryGetTarget(out var r) ? r : null;
            set
            {
                if (value == null)
                {
                    _Current = null;
                }
                else if (_Current != null)
                {
                    _Current.SetTarget(value);
                }
                else
                {
                    _Current = new WeakReference<PushHandlerService>(value);
                }
            }
        }

        public static string RegistrationID { get; private set; }

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
            Current = this;

            Log.Info(GcmBroadcastReceiver.TAG, "GCM Message Received!: ");

            var channelName = intent.Extras.GetString("title");
            var message = intent.Extras.GetString("alert");

            if (!string.IsNullOrEmpty(message))
            {
                var m = new Message()
                {
                    Channel = new Channel()
                    {
                        ChannelName = channelName
                    },
                    Profile = new Profile(),
                    HtmlContent = message,
                    PlainTextContent = message,
                    RawContent = message,
                };
                m.Avatar = m.Profile.Avatar = intent.Extras.GetString("licon");

                var json = intent.Extras.GetString("custom");

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var jo = JObject.Parse(json);
                        var a = jo.Property("a")?.Value as JObject;

                        if (a != null)
                        {
                            var p = a.Property("profile")?.Value as JObject;

                            m.Profile.Id = p?.Property("id")?.Value?.Value<string>();
                            m.Profile.ScreenName = p?.Property("screen_name")?.Value?.Value<string>();
                            m.DisplayName = m.Profile.DisplayName = p?.Property("display_name")?.Value?.Value<string>();

                            m.Id = a.Property("message_id")?.Value?.Value<int>() ?? 0;
                            m.Channel.Id = a.Property("channel_id")?.Value?.Value<string>();
                        }
                    }
                    catch { }
                }

                ApplicationViewModel.ReceiveNotification(m);
            }
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
                        TH.Info("Registering...");
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