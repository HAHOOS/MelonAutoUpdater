using MelonLoader;
using System.Drawing;
using System;

namespace MelonAutoUpdater.Search
{
    public class MAULogger
    {
        internal string Name { get; set; }

        public static readonly Color DefaultMAUSEColor = Color.Cyan;
        public static readonly Color DefaultTextColor = Color.LightGray;

        private readonly MelonLogger.Instance logger = Core.logger;

        public MAULogger(string Name)
        {
            this.Name = Name;
        }

        public void Msg(object obj) => InternalMsg(DefaultMAUSEColor, DefaultTextColor, Name, obj.ToString());

        public void Msg(string txt) => InternalMsg(DefaultMAUSEColor, DefaultTextColor, Name, txt);

        public void Msg(string txt, params object[] args) => InternalMsg(DefaultMAUSEColor, DefaultTextColor, Name, string.Format(txt, args));

        public void Msg(Color txt_color, object obj) => InternalMsg(DefaultMAUSEColor, txt_color, Name, obj.ToString());

        public void Msg(Color txt_color, string txt) => InternalMsg(DefaultMAUSEColor, txt_color, Name, txt);

        public void Msg(Color txt_color, string txt, params object[] args) => InternalMsg(DefaultMAUSEColor, txt_color, Name, string.Format(txt, args));

        public void Warning(object obj) => InternalWarning(Name, obj.ToString());

        public void Warning(string txt) => InternalWarning(Name, txt);

        public void Warning(string txt, params object[] args) => InternalWarning(Name, string.Format(txt, args));

        public void Error(object obj) => InternalError(Name, obj.ToString());

        public void Error(string txt) => InternalError(Name, txt);

        public void Error(string txt, params object[] args) => InternalError(Name, string.Format(txt, args));

        public void Error(string txt, Exception ex) => InternalError(Name, $"{txt}\n{ex}");

        public void BigError(string txt) => InternalBigError(Name, txt);

        internal void InternalMsg(Color extColor, Color textColor, string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext.Pastel(extColor)}]";
            logger.Msg($"{extString} {text.Pastel(textColor)}");
        }

        internal void InternalWarning(string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext}]";
            logger.Warning($"{extString} {text}");
        }

        internal void InternalError(string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext}]";
            logger.Error($"{extString} {text}");
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