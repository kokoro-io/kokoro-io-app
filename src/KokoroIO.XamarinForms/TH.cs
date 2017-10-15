using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Azure.Mobile.Analytics;

namespace KokoroIO.XamarinForms
{
    internal static class TH
    {
#if DEBUG
        private sealed class ScopeDisposable : IDisposable
        {
            private readonly string _Name;
            private readonly Stopwatch _Stopwatch;

            public ScopeDisposable(string name)
            {
                _Name = name;
                Debug.WriteLine("Begin {0}", (object)_Name);
                _Stopwatch = new Stopwatch();
                _Stopwatch.Start();
            }

            public void Dispose()
            {
                _Stopwatch.Stop();
                Debug.WriteLine("End {0} in {1:0}ms", _Name, _Stopwatch.Elapsed.TotalMilliseconds);
            }
        }
#endif

        public static IDisposable BeginScope(string name)
        {
#if DEBUG
            return new ScopeDisposable(name);
#else
            return null;
#endif
        }

        public static void TraceError(string message)
        {
            Debug.WriteLine(message);

            Analytics.TrackEvent(message);
        }

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