using MelonAutoUpdater.Utils;
using MelonLoader;
using Semver;
using System;
using System.Drawing;

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
                logger.MsgPastel(text.Pastel(textColor));
            }
        }

        private static void Internal_MsgPastel(MelonLogger.Instance logger, Color textColor, string text)
        {
            Version MelonLoaderVersion = Core.MLAssembly.GetName().Version;
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