using Knx.Falcon.Logging;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel;

public class MauiLoggerFactory : IFalconLoggerFactory
{
    public IFalconLogger GetLogger(string name) => new MauiLogger();
}

public class MauiLogger : IFalconLogger
{
    public void Debug(object message)
    {
    }

    public void Debug(object message, Exception exception)
    {
    }

    public void DebugFormat(string format, params object[] args)
    {
    }

    public void Error(object message)
    {
    }

    public void Error(object message, Exception exception)
    {
    }

    public void ErrorFormat(string format, params object[] args)
    {
    }

    public void Info(object message)
    {
    }

    public void Info(object message, Exception exception)
    {
    }

    public void InfoFormat(string format, params object[] args)
    {
    }

    public void Warn(object message)
    {
    }

    public void Warn(object message, Exception exception)
    {
    }

    public void WarnFormat(string format, params object[] args)
    {
    }

    public bool IsDebugEnabled => true;
    public bool IsErrorEnabled => true;
    public bool IsInfoEnabled => true;
    public bool IsWarnEnabled => true;
}

