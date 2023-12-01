#define ENV_DEVELOPMENT
using UnityEngine;

static class Loggr
{
    internal static void Error(string error, bool report = true) => LogErrorInternal($"{nameof(FloatParamRandomizerEE)}: {error}.", report);

    // ReSharper disable once UnusedParameter.Local
    static void LogErrorInternal(string text, bool report)
    {
        if(report)
        {
            text += " Please report the issue!";
        }

        #if ENV_DEVELOPMENT
        {
            SuperController.LogError($"{text}\n{new System.Diagnostics.StackTrace()}");
        }
        #else
            {
                SuperController.LogError(text);
            }
        #endif
    }

    internal static void Message(string message) => SuperController.LogMessage($"{nameof(FloatParamRandomizerEE)}: {message}");
}
