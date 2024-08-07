#define ENV_DEVELOPMENT
using System;
using System.Text;

sealed class LogBuilder
{
    const string PREFIX = nameof(FloatParamRandomizerEE);
    readonly StringBuilder _sb = new StringBuilder();

    public void Error(string error, bool report = true)
    {
        Clear();
        _sb.Append(error);
        if(report)
        {
            _sb.Append(". Please report the issue!");
        }
        LogError();
    }

    public void Exception(Exception e)
    {
        _sb.Clear().AppendFormat(e.ToString());
        LogException();
    }

    public void Exception(string message, Exception e)
    {
        _sb.Clear().AppendFormat($"{message}: {e}");
        LogException();
    }

    public void Message(string format, params object[] args)
    {
        Clear();
        _sb.AppendFormat(format, args);
        LogMessage();
    }

    public void Message(string message)
    {
        Clear();
        _sb.Append(message);
        LogMessage();
    }

    public void Debug(string message)
    {
        #if ENV_DEVELOPMENT
        {
            Clear();
            _sb.Append(message);
            LogDebug();
        }
        #endif
    }

    void Clear() => _sb.Clear().AppendFormat("{0}: ", PREFIX);

    void LogError()
    {
        SuperController.LogError(_sb.ToString());
    }

    void LogException()
    {
        _sb.AppendFormat("\n{0}", new System.Diagnostics.StackTrace());
        SuperController.LogError(_sb.ToString());
    }

    void LogMessage() => SuperController.LogMessage(_sb.ToString());

    void LogDebug()
    {
        _sb.Insert(0, "[D] ");
        UnityEngine.Debug.Log(_sb.ToString());
    }
}
