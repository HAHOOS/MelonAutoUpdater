using MelonLoader;
using System.Drawing;
using System;
using MelonAutoUpdater.Utils;
using MelonAutoUpdater.Helper;

namespace MelonAutoUpdater.Search
{
    /// <summary>
    /// Provides logging for MAU Search Extensions
    /// </summary>
    public class MAULogger
    {
        internal string Name { get; set; }

        internal static readonly Color DefaultMAUSEColor = Color.Cyan;
        internal static readonly Color DefaultTextColor = Color.LightGray;

        private readonly MelonLogger.Instance logger = MelonAutoUpdater.logger;

        internal MAULogger(string Name)
        {
            this.Name = Name;
        }

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

        internal void InternalMsg(Color extColor, Color textColor, string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext.Pastel(extColor)}] ";
            logger.Msg($"{extString}{text.Pastel(textColor)}");
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
    }
}