using System;
using System.Diagnostics;
using Unity.Tiny;
namespace Unity.Tiny.Rendering
{
    internal static class RenderDebug
    {
        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogError(object message)
        {
            Debug.LogRelease(message);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogWarning(object message)
        {
            Debug.LogRelease(message);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void Log(object logObject)
        {
            Debug.LogRelease(logObject);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogException(Exception exception)
        {
            Debug.LogReleaseException(exception);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogExceptionAlways(Exception exception)
        {
            Debug.LogReleaseExceptionAlways(exception);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogAlways(object logObject)
        {
            Debug.LogReleaseAlways(logObject);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormatRelease(format, args);
        }

        [Conditional("RENDERING_ENABLE_TRACE")]
        public static void LogFormatAlways(string format, params object[] args)
        {
            Debug.LogFormatAlways(format, args);
        }
    }
}