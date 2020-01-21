using System;
using System.Diagnostics;
using Unity.Tiny.Utils;

namespace Unity.Tiny
{
    internal class StringRingHistory
    {
        public StringRingHistory(int count)
        {
            data = new string[count];
            nextIndex = 0;
        }

        public bool Contains(string s)
        {
            foreach(var d in data) {
                // Temporary fix by validating d != null to bypass IL2CPP bug where string equality does not handle null strings
                if (d != null && d == s)
                    return true;
            }
            return false;
        }

        public void Add(string s) 
        {
            data[nextIndex] = s;
            nextIndex++;
            if (nextIndex == data.Length)
                nextIndex = 0;
        }

        int nextIndex;
        string[] data;
    }

    public static class Debug
    {
        static StringRingHistory history = new StringRingHistory(32); // keep last n-strings

        internal static string MessageObjectToString(object message)
        {
            if (message == null)
                return "null (null message, maybe a format which is unsupported?)";
            if (message is string stringMessage)
                return stringMessage;
            if (message is int intMessage)
                return intMessage.ToString();
            if (message is short shortMessage)
                return shortMessage.ToString();
            if (message is float floatMessage)
                return NumberConverter.FloatToString(floatMessage);
            if (message is double doubleMessage)
                return NumberConverter.DoubleToString(doubleMessage);
            if (message is Exception exc)
                return string.Concat(exc.Message, "\n", exc.StackTrace);

            return "Non-Trivially-Stringable OBJECT logged (Not supported in DOTS C#)";
        }

        public static void LogRelease(object logObject)
        {
            var log = MessageObjectToString(logObject);
            if (history.Contains(log))
                return;

            LogOutputString(log);
            history.Add(log);
        }

        // bypass history de-duplication
        public static void LogReleaseAlways(object logObject)
        {
            var log = MessageObjectToString(logObject);
            LogOutputString(log);
        }

        public static void LogFormatRelease(string format, params object[] args)
        {
            var result = StringFormatter.Format(format, args);
            LogRelease(result);
        }

        // bypass history de-duplication
        public static void LogFormatReleaseAlways(string format, params object[] args)
        {
            var result = StringFormatter.Format(format, args);
            LogReleaseAlways(result);
        }

        public static void LogReleaseException(Exception exception)
        {
            LogRelease(exception);
        }

        public static void LogReleaseExceptionAlways(Exception exception)
        {
            LogReleaseAlways(exception);
        }

        /// <summary>
        /// Writes an object's ToString to stdout as a error.
        /// This function is affected by log history/spam filtering, and will be suppressed if it Logs too frequently
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogError(object message)
        {
            LogRelease(message);
        }

        /// <summary>
        /// Writes an object's ToString to stdout as a warning.
        /// This function is affected by log history/spam filtering, and will be suppressed if it Logs too frequently
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogWarning(object message)
        {
            LogRelease(message);
        }

        /// <summary>
        /// Writes an object's ToString to stdout.
        /// This function is affected by log history/spam filtering, and will be suppressed if it Logs too frequently
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(object logObject)
        {
            LogRelease(logObject);
        }

        /// <summary>
        /// Writes a formatted string to stdout.
        /// This function is affected by log history/spam filtering, and will be suppressed if it Logs too frequently
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogException(Exception exception)
        {
            LogReleaseException(exception);
        }

        /// <summary>
        /// Writes a formatted string to stdout.
        /// This function is affected by log history/spam filtering, and will be suppressed if it Logs too frequently
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogExceptionAlways(Exception exception)
        {
            LogReleaseExceptionAlways(exception);
        }

        /// <summary>
        /// Writes an object's ToString to stdout.
        /// This function will always log (it is unaffected by log history/spam filtering)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogAlways(object logObject)
        {
            LogReleaseAlways(logObject);
        }

        /// <summary>
        /// Writes a formatted string to stdout.
        /// This function is affected by log history/spam filtering, and will be suppressed if it Logs too frequently
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogFormat(string format, params object[] args)
        {
            LogFormatRelease(format, args);
        }

        /// <summary>
        /// Writes a formatted string to stdout.
        /// This function will always log (it is unaffected by log history/spam filtering)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogFormatAlways(string format, params object[] args)
        {
            LogFormatReleaseAlways(format, args);
        }

        // We just write everything to Console
        internal static void LogOutputString(string message)
        {
            Console.WriteLine(message);
        }
    }
}
