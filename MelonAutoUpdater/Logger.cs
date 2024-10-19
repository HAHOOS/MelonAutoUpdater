using MelonAutoUpdater.Utils;
using System;
using System.Drawing;
using static MelonAutoUpdater.Logger;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class used for Updater, to provide ease to use MelonUpdater in the PluginUpdater project
    /// </summary>
    public class Logger
    {
        public event EventHandler<LogEventArgs> Log;

        internal static readonly Color DefaultTextColor = Color.LightGray;

        internal void Msg(object obj) => InternalMsg(DefaultTextColor, obj.ToString());

        internal void Msg(string txt) => InternalMsg(DefaultTextColor, txt);

        internal void Msg(string txt, params object[] args) => InternalMsg(DefaultTextColor, string.Format(txt, args));

        internal void Msg(Color txt_color, object obj) => InternalMsg(txt_color, obj.ToString());

        internal void Msg(Color txt_color, string txt) => InternalMsg(txt_color, txt);

        internal void Msg(Color txt_color, string txt, params object[] args) => InternalMsg(txt_color, string.Format(txt, args));

        internal void Warning(object obj) => InternalWarning(obj.ToString());

        internal void Warning(string txt) => InternalWarning(txt);

        internal void Warning(string txt, params object[] args) => InternalWarning(string.Format(txt, args));

        internal void Error(object obj) => InternalError(obj.ToString());

        internal void Error(string txt) => InternalError(txt);

        internal void Error(string txt, params object[] args) => InternalError(string.Format(txt, args));

        internal void Error(string txt, Exception ex) => InternalError($"{txt}\n{ex}");

        internal void DebugMsg(object obj) => Internal_DebugMsg(DefaultTextColor, obj.ToString());

        internal void DebugMsg(string txt) => Internal_DebugMsg(DefaultTextColor, txt);

        internal void DebugMsg(string txt, params object[] args) => Internal_DebugMsg(DefaultTextColor, string.Format(txt, args));

        internal void DebugMsg(ConsoleColor txt_color, object obj) => Internal_DebugMsg(LoggerUtils.ConsoleColorToDrawingColor(txt_color), obj.ToString());

        internal void DebugMsg(ConsoleColor txt_color, string txt) => Internal_DebugMsg(LoggerUtils.ConsoleColorToDrawingColor(txt_color), txt);

        internal void DebugMsg(ConsoleColor txt_color, string txt, params object[] args) => Internal_DebugMsg(LoggerUtils.ConsoleColorToDrawingColor(txt_color), string.Format(txt, args));

        internal void DebugMsg(Color txt_color, object obj) => Internal_DebugMsg(txt_color, obj.ToString());

        internal void DebugMsg(Color txt_color, string txt) => Internal_DebugMsg(txt_color, txt);

        internal void DebugMsg(Color txt_color, string txt, params object[] args) => Internal_DebugMsg(txt_color, string.Format(txt, args));

        internal void DebugWarning(object obj) => Internal_DebugWarning(obj.ToString());

        internal void DebugWarning(string txt) => Internal_DebugWarning(txt);

        internal void DebugWarning(string txt, params object[] args) => Internal_DebugWarning(string.Format(txt, args));

        internal void DebugError(object obj) => Internal_DebugError(obj.ToString());

        internal void DebugError(string txt) => Internal_DebugError(txt);

        internal void DebugError(string txt, params object[] args) => Internal_DebugError(string.Format(txt, args));

        internal void DebugError(string txt, Exception ex) => Internal_DebugError($"{txt}\n{ex}");

        internal void InternalMsg(Color txt_color, string txt)
        {
            if (txt_color == DefaultTextColor) OnLog(LogSeverity.MESSAGE, txt);
            else OnLog(LogSeverity.MESSAGE, txt.Pastel(txt_color));
        }

        internal void InternalWarning(string txt)
        {
            OnLog(LogSeverity.WARNING, txt);
        }

        internal void InternalError(string txt)
        {
            OnLog(LogSeverity.ERROR, txt);
        }

        internal void Internal_DebugMsg(Color textColor, string text)
        {
            if (textColor == DefaultTextColor) OnLog(LogSeverity.DEBUG, text);
            else OnLog(LogSeverity.DEBUG, text.Pastel(textColor));
        }

        internal void Internal_DebugWarning(string text)
        {
            OnLog(LogSeverity.DEBUG_WARNING, text);
        }

        internal void Internal_DebugError(string text)
        {
            OnLog(LogSeverity.DEBUG_ERROR, text);
        }

        /// <summary>
        /// Triggers the <see cref="Log"/> event
        /// </summary>

        protected virtual void OnLog(LogSeverity severity, string message)
        {
            Log?.Invoke(this, new LogEventArgs(message, severity));
        }

        /// <summary>
        /// <see langword="enum"/> used to describe the severity of the log
        /// </summary>
        public enum LogSeverity
        {
            /// <summary>
            /// The log will be sent as a message
            /// </summary>
            MESSAGE,

            /// <summary>
            /// The log will be sent as a warning
            /// </summary>
            WARNING,

            /// <summary>
            /// The log will be sent as a error
            /// </summary>
            ERROR,

            /// <summary>
            /// The log will be sent as a debug message, which means only when the plugin is in DEBUG mode it will be displayed
            /// </summary>
            DEBUG,

            /// <summary>
            /// The log will be sent as a debug warning, which means only when the plugin is in DEBUG mode it will be displayed
            /// </summary>
            DEBUG_WARNING,

            /// <summary>
            /// The log will be sent as a debug error, which means only when the plugin is in DEBUG mode it will be displayed
            /// </summary>
            DEBUG_ERROR,
        }
    }

    /// <summary>
    /// Event arguments for the event Log in <see cref="NuGet"/>
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Message in the log
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Severity of the log
        /// </summary>
        public LogSeverity Severity { get; set; }

        /// <summary>
        /// Creates new instance of <see cref="LogEventArgs"/>
        /// </summary>
        /// <param name="message"><inheritdoc cref="Message"/></param>
        /// <param name="severity"><inheritdoc cref="Severity"/></param>
        public LogEventArgs(string message, LogSeverity severity)
        {
            Message = message;
            Severity = severity;
        }
    }
}