using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AppCenter.Analytics;

namespace KokoroIO.XamarinForms
{
    internal static class TH
    {
#if WINDOWS_UWP
        private static void ErrorCore(string message)
            => Debug.WriteLine(message);

        private static void ErrorCore(string format, params object[] args)
            => Debug.WriteLine(format, args);

        private static void WarnCore(string message)
            => Debug.WriteLine(message);

        private static void WarnCore(string format, params object[] args)
            => Debug.WriteLine(format, args);

        private static void InfoCore(string message)
            => Debug.WriteLine(message);

        private static void InfoCore(string format, params object[] args)
            => Debug.WriteLine(format, args);
#else

        private static void ErrorCore(string message)
            => System.Diagnostics.Trace.TraceError(message);

        private static void ErrorCore(string format, params object[] args)
            => System.Diagnostics.Trace.TraceError(format, args);

        private static void WarnCore(string message)
            => System.Diagnostics.Trace.TraceWarning(message);

        private static void WarnCore(string format, params object[] args)
            => System.Diagnostics.Trace.TraceWarning(format, args);

        private static void InfoCore(string message)
            => System.Diagnostics.Trace.TraceInformation(message);

        private static void InfoCore(string format, params object[] args)
            => System.Diagnostics.Trace.TraceInformation(format, args);

#endif

        public static void Error(string message)
        {
            ErrorCore(message);
            Analytics.TrackEvent(message);
        }

        public static void Error(string format, params object[] args)
        {
            ErrorCore(format, args);
            TrackFormat(format, args);
        }

        public static void Warn(string message)
        {
            WarnCore(message);
            Analytics.TrackEvent(message);
        }

        public static void Warn(string format, params object[] args)
        {
            WarnCore(format, args);
            TrackFormat(format, args);
        }

        public static void Info(string message)
        {
            InfoCore(message);
            Analytics.TrackEvent(message);
        }

        public static void Info(string format, params object[] args)
        {
            InfoCore(format, args);
            TrackFormat(format, args);
        }

        private static void TrackFormat(string format, object[] args)
        {
            var dic = new Dictionary<string, string>()
            {
                ["Message"] = string.Format(format, args)
            };

            for (var i = 0; i < 4; i++)
            {
                var v = args?.ElementAtOrDefault(i)?.ToString();
                if (v != null)
                {
                    dic[i == 0 ? "arg0" : i == 1 ? "arg1" : i == 2 ? "arg2" : i == 3 ? "arg3" : ("arg" + i)] = v;
                }
            }

            Analytics.TrackEvent(format, dic);
        }


        private sealed class ScopeDisposable : IDisposable
        {
            private readonly string _Name;
            private readonly Stopwatch _Stopwatch;

            public ScopeDisposable(string name)
            {
                _Name = name;
                InfoCore("Begin {0}", (object)_Name);
                _Stopwatch = new Stopwatch();
                _Stopwatch.Start();
            }

            public void Dispose()
            {
                _Stopwatch.Stop();
                InfoCore("End {0} in {1:0}ms", _Name, _Stopwatch.Elapsed.TotalMilliseconds);
                Analytics.TrackEvent(_Name, new Dictionary<string, string>()
                {
                    ["Elapsed"] = _Stopwatch.ElapsedMilliseconds.ToString("0\"ms\"")
                });
            }
        }

        public static IDisposable BeginScope(string name)
            => new ScopeDisposable(name);

        public static void Warn(this Exception exception, string eventName)
        {
            WarnCore("Exception catched at {0}: {1}", eventName, exception);

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
        public static void Error(this Exception exception, string eventName)
        {
            ErrorCore("Exception catched at {0}: {1}", eventName, exception);

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