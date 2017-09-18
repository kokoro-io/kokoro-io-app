using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Azure.Mobile.Analytics;

namespace KokoroIO.XamarinForms
{
    internal static class TH
    {
        public static void Trace(this Exception exception, string eventName)
        {
            Debug.WriteLine("Exception catched at {0}: {1}", eventName, exception);

            var bex = exception.GetBaseException();

            Analytics.TrackEvent(eventName, new Dictionary<string, string>()
            {
                ["Exception Type"] = exception.GetType().FullName,
                ["Exception Message"] = exception.Message,
                ["Base Exception Type"] = bex?.GetType().FullName,
                ["Base Exception Message"] = bex?.Message,
                ["Base Exception StackTrace"] = bex?.StackTrace,
            });
        }
    }
}
