using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CodeDrive.Core
{
    /// <summary>
    /// Suppresses harmless Unity Editor SendMouseEvents / Frustum assertion log spam
    /// that causes severe main thread lag and screen stuttering when Game View is scaled or letterboxed.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class FrustumAssertionSuppressor
    {
        private static bool _isInitialized = false;

#if UNITY_EDITOR
        static FrustumAssertionSuppressor()
        {
            InitFilter();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnPlay()
        {
            InitFilter();
        }

        public static void InitFilter()
        {
            if (_isInitialized) return;

            var defaultLogHandler = Debug.unityLogger.logHandler;
            if (!(defaultLogHandler is SuppressLogHandler))
            {
                Debug.unityLogger.logHandler = new SuppressLogHandler(defaultLogHandler);
            }
            _isInitialized = true;
        }

        private class SuppressLogHandler : ILogHandler
        {
            private readonly ILogHandler _baseHandler;

            public SuppressLogHandler(ILogHandler baseHandler)
            {
                _baseHandler = baseHandler;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (format != null)
                {
                    // Catch all internal Unity SendMouseEvents assertion & frustum log spam
                    if (format.Contains("IsNormalized") ||
                        format.Contains("Screen position out of view frustum") ||
                        format.Contains("SendMouseEvents") ||
                        format.Contains("ray.GetDirection()"))
                    {
                        return; // Ignore and do not write to Console or lock main thread
                    }
                }

                _baseHandler.LogFormat(logType, context, format, args);
            }

            public void LogException(System.Exception exception, Object context)
            {
                _baseHandler.LogException(exception, context);
            }
        }
    }
}
