using MelonAutoUpdater.Utils;
using MelonLoader;
using Semver;
using System;
using System.Drawing;

namespace MelonAutoUpdater.Helper
{
    /// <summary>
    /// Helper class for MelonLogger
    /// </summary>
    public static class MelonLoggerHelper
    {
        internal static readonly Color DefaultTextColor = Color.LightGray;

        #region MsgPastel

        [System.Runtime.CompilerServices.MethodImpl(
    System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void UseMLMsgPastel(MelonLogger.Instance logger, Color textColor, string text)
        {
            if (textColor == DefaultTextColor)
            {
                logger.MsgPastel(text);
            }
            else
            {
                logger.MsgPastel(text.Pastel(textColor));
            }
        }

        private static void Internal_MsgPastel(MelonLogger.Instance logger, Color textColor, string text)
        {
            Version MelonLoaderVersion = MelonAutoUpdater.MLAssembly.GetName().Version;
            if (new SemVersion(MelonLoaderVersion.Major, MelonLoaderVersion.Minor, MelonLoaderVersion.Build) >= new SemVersion(0, 6, 5))
            {
                UseMLMsgPastel(logger, textColor, text);
            }
            else
            {
                if (textColor == DefaultTextColor)
                {
                    logger.Msg(text);
                }
                else
                {
                    logger.Msg(text.Pastel(textColor));
                }
            }
        }

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="logger">Logger that should be used to send the message</param>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, object obj) => Internal_MsgPastel(logger, DefaultTextColor, obj.ToString());

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="logger">Logger that should be used to send the message</param>
        /// <param name="txt">The text that will be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, string txt) => Internal_MsgPastel(logger, DefaultTextColor, txt);

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="logger">Logger that should be used to send the message</param>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, string txt, params object[] args) => Internal_MsgPastel(logger, DefaultTextColor, string.Format(txt, args));

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="logger">Logger that should be used to send the message</param>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, Color txt_color, object obj) => Internal_MsgPastel(logger, txt_color, obj.ToString());

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="logger">Logger that should be used to send the message</param>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, Color txt_color, string txt) => Internal_MsgPastel(logger, txt_color, txt);

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="logger">Logger that should be used to send the message</param>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, Color txt_color, string txt, params object[] args) => Internal_MsgPastel(logger, txt_color, string.Format(txt, args));

        #endregion MsgPastel

        #region Debug

        internal static void Internal_DebugMsg(MelonLogger.Instance logger, Color textColor, string text)
        {
            if (MelonAutoUpdater.Debug)
            {
                if (textColor == DefaultTextColor)
                {
                    logger.Msg(text);
                }
                else
                {
                    logger.Msg(text.Pastel(textColor));
                }
            }
        }

        internal static void Internal_DebugMsgPastel(MelonLogger.Instance logger, Color textColor, string text)
        {
            if (MelonAutoUpdater.Debug)
            {
                if (textColor == DefaultTextColor)
                {
                    logger._MsgPastel(text);
                }
                else
                {
                    logger._MsgPastel(text.Pastel(textColor));
                }
            }
        }

        internal static void Internal_DebugWarning(MelonLogger.Instance logger, string text)
        {
            if (MelonAutoUpdater.Debug)
            {
                logger.Warning(text);
            }
        }

        internal static void Internal_DebugError(MelonLogger.Instance logger, string text)
        {
            if (MelonAutoUpdater.Debug)
            {
                logger.Error(text);
            }
        }

        public static void DebugMsg(this MelonLogger.Instance logger, object obj) => Internal_DebugMsg(logger, DefaultTextColor, obj.ToString());

        public static void DebugMsg(this MelonLogger.Instance logger, string txt) => Internal_DebugMsg(logger, DefaultTextColor, txt);

        public static void DebugMsg(this MelonLogger.Instance logger, string txt, params object[] args) => Internal_DebugMsg(logger, DefaultTextColor, string.Format(txt, args));

        public static void DebugMsg(this MelonLogger.Instance logger, ConsoleColor txt_color, object obj) => Internal_DebugMsg(logger, LoggerUtils.ConsoleColorToDrawingColor(txt_color), obj.ToString());

        public static void DebugMsg(this MelonLogger.Instance logger, ConsoleColor txt_color, string txt) => Internal_DebugMsg(logger, LoggerUtils.ConsoleColorToDrawingColor(txt_color), txt);

        public static void DebugMsg(this MelonLogger.Instance logger, ConsoleColor txt_color, string txt, params object[] args) => Internal_DebugMsg(logger, LoggerUtils.ConsoleColorToDrawingColor(txt_color), string.Format(txt, args));

        public static void DebugMsg(this MelonLogger.Instance logger, Color txt_color, object obj) => Internal_DebugMsg(logger, txt_color, obj.ToString());

        public static void DebugMsg(this MelonLogger.Instance logger, Color txt_color, string txt) => Internal_DebugMsg(logger, txt_color, txt);

        public static void DebugMsg(this MelonLogger.Instance logger, Color txt_color, string txt, params object[] args) => Internal_DebugMsg(logger, txt_color, string.Format(txt, args));

        public static void DebugMsgPastel(this MelonLogger.Instance logger, object obj) => Internal_DebugMsgPastel(logger, DefaultTextColor, obj.ToString());

        public static void DebugMsgPastel(this MelonLogger.Instance logger, string txt) => Internal_DebugMsgPastel(logger, DefaultTextColor, txt);

        public static void DebugMsgPastel(this MelonLogger.Instance logger, string txt, params object[] args) => Internal_DebugMsgPastel(logger, DefaultTextColor, string.Format(txt, args));

        public static void DebugMsgPastel(this MelonLogger.Instance logger, ConsoleColor txt_color, object obj) => Internal_DebugMsgPastel(logger, LoggerUtils.ConsoleColorToDrawingColor(txt_color), obj.ToString());

        public static void DebugMsgPastel(this MelonLogger.Instance logger, ConsoleColor txt_color, string txt) => Internal_DebugMsgPastel(logger, LoggerUtils.ConsoleColorToDrawingColor(txt_color), txt);

        public static void DebugMsgPastel(this MelonLogger.Instance logger, ConsoleColor txt_color, string txt, params object[] args) => Internal_DebugMsgPastel(logger, LoggerUtils.ConsoleColorToDrawingColor(txt_color), string.Format(txt, args));

        public static void DebugMsgPastel(this MelonLogger.Instance logger, Color txt_color, object obj) => Internal_DebugMsgPastel(logger, txt_color, obj.ToString());

        public static void DebugMsgPastel(this MelonLogger.Instance logger, Color txt_color, string txt) => Internal_DebugMsgPastel(logger, txt_color, txt);

        public static void DebugMsgPastel(this MelonLogger.Instance logger, Color txt_color, string txt, params object[] args) => Internal_DebugMsgPastel(logger, txt_color, string.Format(txt, args));

        public static void DebugWarning(this MelonLogger.Instance logger, object obj) => Internal_DebugWarning(logger, obj.ToString());

        public static void DebugWarning(this MelonLogger.Instance logger, string txt) => Internal_DebugWarning(logger, txt);

        public static void DebugWarning(this MelonLogger.Instance logger, string txt, params object[] args) => Internal_DebugWarning(logger, string.Format(txt, args));

        public static void DebugError(this MelonLogger.Instance logger, object obj) => Internal_DebugError(logger, obj.ToString());

        public static void DebugError(this MelonLogger.Instance logger, string txt) => Internal_DebugError(logger, txt);

        public static void DebugError(this MelonLogger.Instance logger, string txt, params object[] args) => Internal_DebugError(logger, string.Format(txt, args));

        public static void DebugError(this MelonLogger.Instance logger, string txt, Exception ex) => Internal_DebugError(logger, $"{txt}\n{ex}");

        #endregion Debug
    }
}