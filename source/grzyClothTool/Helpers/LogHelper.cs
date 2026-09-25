using grzyClothTool.Views;
using System;
using System.Windows;

namespace grzyClothTool.Helpers;

public class LogMessageEventArgs : EventArgs
{
    public string TypeIcon { get; set; }
    public string Message { get; set; }
}

public static class LogHelper
{
    static LogHelper()
    {
        LocalizationHelper.LanguageChanged += (_, _) => RelocalizeOpenMessages();
    }

    private static LogWindow _logWindow;
    public static event EventHandler<LogMessageEventArgs> LogMessageCreated;

    private static void RelocalizeOpenMessages()
    {
        LogWindow? window = _logWindow;
        if (window == null || window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
            return;

        void Relocalize()
        {
            foreach (LogMessage logMessage in window.LogMessages)
            {
                logMessage.Message = LocalizationHelper.Translate(logMessage.OriginalMessage);
            }
        }

        if (window.Dispatcher.CheckAccess())
        {
            Relocalize();
        }
        else
        {
            window.Dispatcher.BeginInvoke(new Action(Relocalize));
        }
    }

    public static void Init()
    {
        _logWindow = new LogWindow();
    }

    public static void Log(string message, LogType logtype = LogType.Info)
    {
        LogWindow? window = _logWindow;
        if (window == null)
            return;

        var dispatcher = window.Dispatcher;
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        void AppendLog()
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var type = GetLogTypeIcon(logtype);
            string localizedMessage = LocalizationHelper.Translate(message);

            window.LogMessages.Add(new LogMessage
            {
                TypeIcon = type,
                OriginalMessage = message,
                Message = localizedMessage,
                Timestamp = timestamp
            });
            LogMessageCreated?.Invoke(window, new LogMessageEventArgs { TypeIcon = type, Message = localizedMessage });
        }

        if (dispatcher.CheckAccess())
        {
            AppendLog();
        }
        else
        {
            dispatcher.BeginInvoke(new Action(AppendLog));
        }
    }

    public static string GetLogTypeIcon(LogType type)
    {
        return type switch
        {
            LogType.Info => "Check",
            LogType.Warning => "WarningOutline",
            LogType.Error => "Close",
            _ => "Info"
        };
    }

    public static void OpenLogWindow()
    {
        if (_logWindow == null)
        {
            return;
        }

        if (!_logWindow.IsVisible)
        {
            _logWindow.Owner = Application.Current?.MainWindow;
            _logWindow.Show();
        }
        else
        {
            _logWindow.Activate();
        }
    }

    public static void Close()
    {
        LogWindow? window = _logWindow;
        _logWindow = null;
        if (window == null)
            return;

        window.Closing -= window.LogWindow_Closing;
        if (!window.Dispatcher.HasShutdownStarted && !window.Dispatcher.HasShutdownFinished)
        {
            window.Close();
        }
    }
}
