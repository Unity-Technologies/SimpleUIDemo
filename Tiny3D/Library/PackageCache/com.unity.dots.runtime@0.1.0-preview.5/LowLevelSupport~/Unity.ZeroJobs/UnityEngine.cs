using System;
using System.Runtime.InteropServices;
#if !NET_DOTS
using System.Text.RegularExpressions;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEngine
{
    public static class Debug
    {
        internal static string lastLog;
        internal static string lastWarning;
        internal static string lastError;

        public static void LogError(object message)
        {
            if (message == null)
                lastError = "LogError: null (null message, maybe a format which is unsupported?)";
            else if (message is string strMessage)
                lastError = strMessage;
            else
                lastError = "LogError: NON-String OBJECT LOGGED";
            Console.WriteLine(lastError);
        }

        public static void LogWarning(string message)
        {
            lastWarning = message;
            Console.WriteLine(message);
        }

        public static void Log(string message)
        {
            lastLog = message;
            Console.WriteLine(message);
        }

        public static void Log(int message) => Log(message.ToString());
        public static void Log(float message) => Log(message.ToString());

        public static void LogException(Exception exception)
        {
            lastLog = "Exception";
            Console.WriteLine(exception.Message + "\n" + exception.StackTrace);
        }
    }

    public class Component {}

    public class Random
    {
        public static void InitState(int state)
        {
        }

        public static int Range(int one, int two)
        {
            return one;
        }
    }

    // The type of the log message in the delegate registered with Application.RegisterLogCallback.
    public enum LogType
    {
        // LogType used for Errors.
        Error = 0,
        // LogType used for Asserts. (These indicate an error inside Unity itself.)
        Assert = 1,
        // LogType used for Warnings.
        Warning = 2,
        // LogType used for regular log messages.
        Log = 3,
        // LogType used for Exceptions.
        Exception = 4
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ExecuteAlwaysAttribute : Attribute
    {
        public ExecuteAlwaysAttribute()
        {
        }
    }

    public static class Time
    {
        [DllImport("lib_unity_zerojobs")]
        public static extern long Time_GetTicksMicrosecondsMonotonic();

        public static float time => Time_GetTicksMicrosecondsMonotonic() / 1_000_000.0f;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip)
        {
        }
    }

    public sealed class SerializeField : Attribute
    {
    }
}
