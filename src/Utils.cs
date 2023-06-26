public static class Utils
{
    public static void LogError(string message) =>
        SuperController.LogError(Format(message));

    public static void LogMessage(string message) =>
        SuperController.LogMessage(Format(message));

    static string Format(string message) =>
        $"{nameof(FloatParamRandomizerEE)} {FloatParamRandomizerEE.VERSION}: {message}";
}
