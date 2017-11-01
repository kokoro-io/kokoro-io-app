using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Azure.Mobile.Analytics;

namespace KokoroIO.XamarinForms
{
    internal static class TH
    {
#if WINDOWS_UWP
        public static void Error(string message)
            => Debug.TraceError(message);

        public static void Error(string format, params object[] args)
            => Debug.TraceError(format, args);


        public static void Warn(string message)
            => Debug.TraceWarning(message);

        public static void Warn(string format, params object[] args)
            => Debug.TraceWarning(format, args);


        public static void Info(string message)
            => Debug.TraceInformation(message);

        public static void Info(string format, params object[] args)
            => Debug.TraceInformation(format, args);
#else
        public static void Error(string message)
            => System.Diagnostics.Trace.TraceError(message);

        public static void Error(string format, params object[] args)
            => System.Diagnostics.Trace.TraceError(format, args);


        public static void Warn(string message)
            => System.Diagnostics.Trace.TraceWarning(message);

        public static void Warn(string format, params object[] args)
            => System.Diagnostics.Trace.TraceWarning(format, args);


        public static void Info(string message)
            => System.Diagnostics.Trace.TraceInformation(message);

        public static void Info(string format, params object[] args)
            => System.Diagnostics.Trace.TraceInformation(format, args);
#endif

#if DEBUG
        private sealed class ScopeDisposable : IDisposable
        {
            private readonly string _Name;
            private readonly Stopwatch _Stopwatch;

            public ScopeDisposable(string name)
            {
                _Name = name;
                Info("Begin {0}", (object)_Name);
                _Stopwatch = new Stopwatch();
                _Stopwatch.Start();
            }

            public void Dispose()
            {
                _Stopwatch.Stop();
                Info("End {0} in {1:0}ms", _Name, _Stopwatch.Elapsed.TotalMilliseconds);
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
            Warn(message);

            Analytics.TrackEvent(message);
        }

        public static void Trace(this Exception exception, string eventName)
        {
            Warn("Exception catched at {0}: {1}", eventName, exception);

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