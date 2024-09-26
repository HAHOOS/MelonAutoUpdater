using MelonLoader;
using Semver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonAutoUpdater.Helper
{
    public static class MelonLoggerHelper
    {
        internal static readonly Color DefaultTextColor = Color.LightGray;

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
                logger.MsgPastel(textColor, text);
            }
        }

        private static void Internal_MsgPastel(MelonLogger.Instance logger, Color textColor, string text)
        {
            if (SemVersion.Parse(MelonLoader.BuildInfo.Version) >= new SemVersion(0, 6, 5))
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
                    logger.Msg(textColor, text);
                }
            }
        }

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, object obj) => Internal_MsgPastel(logger, DefaultTextColor, obj.ToString());

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, string txt) => Internal_MsgPastel(logger, DefaultTextColor, txt);

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, string txt, params object[] args) => Internal_MsgPastel(logger, DefaultTextColor, string.Format(txt, args));

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="obj">Object that will be converted to string to be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, Color txt_color, object obj) => Internal_MsgPastel(logger, txt_color, obj.ToString());

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, Color txt_color, string txt) => Internal_MsgPastel(logger, txt_color, txt);

        /// <summary>
        /// Send a message to console, as well as removing pastel
        /// </summary>
        /// <param name="txt_color">Color of the text</param>
        /// <param name="txt">The text that will be sent</param>
        /// <param name="args">The arguments in text</param>
        public static void _MsgPastel(this MelonLogger.Instance logger, Color txt_color, string txt, params object[] args) => Internal_MsgPastel(logger, txt_color, string.Format(txt, args));
    }
}