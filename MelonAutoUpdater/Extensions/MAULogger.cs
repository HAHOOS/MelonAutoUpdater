extern alias ml065;

using ml065.MelonLoader;
using System.Drawing;
using System;
using MelonAutoUpdater.Utils;
using MelonAutoUpdater.Helper;

namespace MelonAutoUpdater.Extensions
{
    /// <summary>
    /// Provides logging for MAU Search Extensions
    /// </summary>
    public class MAULogger
    {
        #region Internal

        internal string Name { get; set; }

        internal static readonly Color DefaultMAUSEColor = Color.Cyan;
        internal static readonly Color DefaultTextColor = Color.LightGray;

        internal readonly MelonLogger.Instance logger = MelonAutoUpdater.logger;

        internal MAULogger(string Name, string ID)
        {
            if (string.IsNullOrEmpty(ID)) this.Name = Name;
            else this.Name = $"{ID}:{Name}";
        }

        internal void Internal_DebugMsg(Color textColor, string text)
        {
            if (textColor == DefaultTextColor) logger.DebugMsgPastel(text);
            else logger.DebugMsgPastel(text.Pastel(textColor));
        }

        internal void Internal_DebugMsgPastel(Color textColor, string text)
        {
            if (textColor == DefaultTextColor) logger.DebugMsgPastel(text);
            else logger.DebugMsgPastel(text.Pastel(textColor));
        }

        internal void Internal_DebugWarning(string text)
        {
            logger.DebugWarning(text);
        }

        internal void Internal_DebugError(string text)
        {
            logger.DebugError(text);
        }

        internal void InternalMsg(Color extColor, Color textColor, string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext.Pastel(extColor)}] ";
            logger._MsgPastel($"{extString}{text.Pastel(textColor)}");
        }

        internal void InternalMsgPastel(Color extColor, Color textColor, string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext.Pastel(extColor)}] ";
            logger._MsgPastel($"{extString}{text.Pastel(textColor)}");
        }

        internal void InternalWarning(string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext}] ";
            logger.Warning($"{extString}{text}");
        }

        internal void InternalError(string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext}] ";
            logger.Error($"{extString}{text}");
        }

        internal void InternalBigError(string ext, string txt)
        {
            InternalError(ext, new string('=', 50));
            foreach (var line in txt.Split('\n'))
                InternalError(ext, line);
            InternalError(ext, new string('=', 50));
        }

        #endregion Internal

        #region Public

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public void Msg(object obj) => InternalMsg(DefaultMAUSEColor, DefaultTextColor, Name, obj.ToString());

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        public void Msg(string txt) => InternalMsg(DefaultMAUSEColor, DefaultTextColor, Name, txt);

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public void Msg(string txt, params object[] args) => InternalMsg(DefaultMAUSEColor, DefaultTextColor, Name, string.Format(txt, args));

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public void Msg(Color txt_color, object obj) => InternalMsg(DefaultMAUSEColor, txt_color, Name, obj.ToString());

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        public void Msg(Color txt_color, string txt) => InternalMsg(DefaultMAUSEColor, txt_color, Name, txt);

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public void Msg(Color txt_color, string txt, params object[] args) => InternalMsg(DefaultMAUSEColor, txt_color, Name, string.Format(txt, args));

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public void MsgPastel(object obj) => InternalMsgPastel(DefaultMAUSEColor, DefaultTextColor, Name, obj.ToString());

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        public void MsgPastel(string txt) => InternalMsgPastel(DefaultMAUSEColor, DefaultTextColor, Name, txt);

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public void MsgPastel(string txt, params object[] args) => InternalMsgPastel(DefaultMAUSEColor, DefaultTextColor, Name, string.Format(txt, args));

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public void MsgPastel(Color txt_color, object obj) => InternalMsgPastel(DefaultMAUSEColor, txt_color, Name, obj.ToString());

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        public void MsgPastel(Color txt_color, string txt) => InternalMsgPastel(DefaultMAUSEColor, txt_color, Name, txt);

        /// <summary>
        /// Send a message to console
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public void MsgPastel(Color txt_color, string txt, params object[] args) => InternalMsgPastel(DefaultMAUSEColor, txt_color, Name, string.Format(txt, args));

        /// <summary>
        /// Send a warning to console
        /// </summary>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public void Warning(object obj) => InternalWarning(Name, obj.ToString());

        /// <summary>
        /// Send a warning to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        public void Warning(string txt) => InternalWarning(Name, txt);

        /// <summary>
        /// Send a warning to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public void Warning(string txt, params object[] args) => InternalWarning(Name, string.Format(txt, args));

        /// <summary>
        /// Send an error to console
        /// </summary>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public void Error(object obj) => InternalError(Name, obj.ToString());

        /// <summary>
        /// Send an error to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        public void Error(string txt) => InternalError(Name, txt);

        /// <summary>
        /// Send an error to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public void Error(string txt, params object[] args) => InternalError(Name, string.Format(txt, args));

        /// <summary>
        /// Send an error to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="ex">Exception that will be sent as well</param>
        public void Error(string txt, Exception ex) => InternalError(Name, $"{txt}\n{ex}");

        /// <summary>
        /// Send a big error to console
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        public void BigError(string txt) => InternalBigError(Name, txt);

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugMsg(object obj) => Internal_DebugMsg(DefaultTextColor, obj.ToString());

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        public void DebugMsg(string txt) => Internal_DebugMsg(DefaultTextColor, txt);

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugMsg(string txt, params object[] args) => Internal_DebugMsg(DefaultTextColor, string.Format(txt, args));

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugMsg(ConsoleColor txt_color, object obj) => Internal_DebugMsg(LoggerUtils.ConsoleColorToDrawingColor(txt_color), obj.ToString());

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        public void DebugMsg(ConsoleColor txt_color, string txt) => Internal_DebugMsg(LoggerUtils.ConsoleColorToDrawingColor(txt_color), txt);

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugMsg(ConsoleColor txt_color, string txt, params object[] args) => Internal_DebugMsg(LoggerUtils.ConsoleColorToDrawingColor(txt_color), string.Format(txt, args));

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugMsg(Color txt_color, object obj) => Internal_DebugMsg(txt_color, obj.ToString());

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        public void DebugMsg(Color txt_color, string txt) => Internal_DebugMsg(txt_color, txt);

        /// <summary>
        /// Sends a log if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugMsg(Color txt_color, string txt, params object[] args) => Internal_DebugMsg(txt_color, string.Format(txt, args));

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugMsgPastel(object obj) => Internal_DebugMsgPastel(DefaultTextColor, obj.ToString());

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        public void DebugMsgPastel(string txt) => Internal_DebugMsgPastel(DefaultTextColor, txt);

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugMsgPastel(string txt, params object[] args) => Internal_DebugMsgPastel(DefaultTextColor, string.Format(txt, args));

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugMsgPastel(ConsoleColor txt_color, object obj) => Internal_DebugMsgPastel(LoggerUtils.ConsoleColorToDrawingColor(txt_color), obj.ToString());

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        public void DebugMsgPastel(ConsoleColor txt_color, string txt) => Internal_DebugMsgPastel(LoggerUtils.ConsoleColorToDrawingColor(txt_color), txt);

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugMsgPastel(ConsoleColor txt_color, string txt, params object[] args) => Internal_DebugMsgPastel(LoggerUtils.ConsoleColorToDrawingColor(txt_color), string.Format(txt, args));

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugMsgPastel(Color txt_color, object obj) => Internal_DebugMsgPastel(txt_color, obj.ToString());

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        public void DebugMsgPastel(Color txt_color, string txt) => Internal_DebugMsgPastel(txt_color, txt);

        /// <summary>
        /// Sends a log and removes pastel from if DEBUG mode is enabled
        /// <para>
        /// Note: This is only available on MelonLoader v0.6.5 or later
        /// </para>
        /// </summary>

        /// <param name="txt_color">Color of the message</param>
        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugMsgPastel(Color txt_color, string txt, params object[] args) => Internal_DebugMsgPastel(txt_color, string.Format(txt, args));

        /// <summary>
        /// Sends a warning in logs from if DEBUG mode is enabled
        /// </summary>

        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugWarning(object obj) => Internal_DebugWarning(obj.ToString());

        /// <summary>
        /// Sends a warning in logs from if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        public void DebugWarning(string txt) => Internal_DebugWarning(txt);

        /// <summary>
        /// Sends a warning in logs if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugWarning(string txt, params object[] args) => Internal_DebugWarning(string.Format(txt, args));

        /// <summary>
        /// Sends an error in logs if DEBUG mode is enabled
        /// </summary>

        /// <param name="obj">Object that will be converted to string and sent</param>
        public void DebugError(object obj) => Internal_DebugError(obj.ToString());

        /// <summary>
        /// Sends an error in logs if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        public void DebugError(string txt) => Internal_DebugError(txt);

        /// <summary>
        /// Sends an error in logs if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        /// <param name="args">Arguments for the text</param>
        public void DebugError(string txt, params object[] args) => Internal_DebugError(string.Format(txt, args));

        /// <summary>
        /// Sends an error in logs if DEBUG mode is enabled
        /// </summary>

        /// <param name="txt">Text that will be sent</param>
        /// <param name="ex">Exception to be associated with the message</param>
        public void DebugError(string txt, Exception ex) => Internal_DebugError($"{txt}\n{ex}");

        #endregion Public
    }
}