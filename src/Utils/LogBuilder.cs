#define ENV_DEVELOPMENT
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

sealed class LogBuilder
{
    readonly string _prefix;
    readonly StringBuilder _sb = new StringBuilder();

    public LogBuilder(string moduleName = null)
    {
        #if ENV_DEVELOPMENT
        {
            _sb.AppendFormat("[{0}] ", Guid.NewGuid().ToString().Substring(0, 4));
        }
        #endif

        if(string.IsNullOrEmpty(moduleName))
        {
            _sb.AppendFormat("{0} v{1}", nameof(FloatParamRandomizerEE), FloatParamRandomizerEE.VERSION);
        }
        else
        {
            _sb.AppendFormat("{0}.{1} v{2}", nameof(FloatParamRandomizerEE), moduleName, FloatParamRandomizerEE.VERSION);
        }

        _prefix = _sb.ToString();
    }

    public void Error(string format, params object[] args) => Clear()
        .AppendFormat(format, args)
        .Append(". Please report the issue!")
        .LogError();

    public void Error(string error) => Clear()
        .Append(error)
        .Append(". Please report the issue!")
        .LogError();

    public void ErrorNoReport(string format, params object[] args) => Clear()
        .AppendFormat(format, args)
        .LogError();

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void ErrorNoReport(string error) => Clear()
        .Append(error)
        .LogError();

    public void Message(string format, params object[] args) => Clear().AppendFormat(format, args).LogMessage();
    public void Message(string message) => Clear().Append(message).LogMessage();

    LogBuilder Clear()
    {
        _sb.Clear().AppendFormat("{0}: ", _prefix);
        return this;
    }

    LogBuilder AppendFormat(string format, params object[] args)
    {
        try
        {
            _sb.AppendFormat(format, args);
        }
        catch(Exception e)
        {
            SuperController.LogError($"Naturalis: LogBuilder {e.GetType()}: {e.Message}. Format: \"{format}\", args: [{args.ToPrettyString(", ")}]");
        }

        return this;
    }

    LogBuilder Append(string value)
    {
        _sb.Append(value);
        return this;
    }

    void LogError()
    {
        #if ENV_DEVELOPMENT
        {
            AppendFormat("\n{0}", new System.Diagnostics.StackTrace());
        }
        #endif

        SuperController.LogError(_sb.ToString());
    }

    void LogMessage() => SuperController.LogMessage(_sb.ToString());
}
