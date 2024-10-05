using MelonAutoUpdater.Helper;
using MelonAutoUpdater.Utils;
using System;
using System.Drawing;

namespace MelonAutoUpdater
{
    /// <summary>
    /// Class used for Updater, to provide ease to use MelonUpdater in the PluginUpdater project
    /// </summary>
    internal class Logger
    {
        internal static readonly Color DefaultTextColor = Color.Gray;

        internal static void Msg(object obj) => InternalMsg(DefaultTextColor, obj.ToString());

        internal static void Msg(string txt) => InternalMsg(DefaultTextColor, txt);

        internal static void Msg(string txt, params object[] args) => InternalMsg(DefaultTextColor, string.Format(txt, args));

        internal static void Msg(Color txt_color, object obj) => InternalMsg(txt_color, obj.ToString());

        internal static void Msg(Color txt_color, string txt) => InternalMsg(txt_color, txt);

        internal static void Msg(Color txt_color, string txt, params object[] args) => InternalMsg(txt_color, string.Format(txt, args));

        internal static void MsgPastel(object obj) => InternalMsgPastel(DefaultTextColor, obj.ToString());

        internal static void MsgPastel(string txt) => InternalMsgPastel(DefaultTextColor, txt);

        internal static void MsgPastel(string txt, params object[] args) => InternalMsgPastel(DefaultTextColor, string.Format(txt, args));

        internal static void MsgPastel(Color txt_color, object obj) => InternalMsgPastel(txt_color, obj.ToString());

        internal static void MsgPastel(Color txt_color, string txt) => InternalMsgPastel(txt_color, txt);

        internal static void MsgPastel(Color txt_color, string txt, params object[] args) => InternalMsgPastel(txt_color, string.Format(txt, args));

        internal static void Warning(object obj) => InternalWarning(obj.ToString());

        internal static void Warning(string txt) => InternalWarning(txt);

        internal static void Warning(string txt, params object[] args) => InternalWarning(string.Format(txt, args));

        internal static void Error(object obj) => InternalError(obj.ToString());

        internal static void Error(string txt) => InternalError(txt);

        internal static void Error(string txt, params object[] args) => InternalError(string.Format(txt, args));

        internal static void Error(string txt, Exception ex) => InternalError($"{txt}\n{ex}");

        internal static void InternalMsg(Color txt_color, string txt)
        {
            if (txt_color == DefaultTextColor) MelonAutoUpdater.logger.Msg(txt);
            else MelonAutoUpdater.logger.Msg(txt.Pastel(txt_color));
        }

        internal static void InternalMsgPastel(Color txt_color, string txt)
        {
            MelonAutoUpdater.logger._MsgPastel(txt_color, txt);
        }

        internal static void InternalWarning(string txt)
        {
            MelonAutoUpdater.logger.Warning(txt);
        }

        internal static void InternalError(string txt)
        {
            MelonAutoUpdater.logger.Warning(txt);
        }
    }
}