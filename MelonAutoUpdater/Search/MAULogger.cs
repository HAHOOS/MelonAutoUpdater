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

        public void Msg(object obj) => _Msg(DefaultMAUSEColor, DefaultTextColor, Name, obj.ToString());

        public void Msg(string txt) => _Msg(DefaultMAUSEColor, DefaultTextColor, Name, txt);

        public void Msg(string txt, params object[] args) => _Msg(DefaultMAUSEColor, DefaultTextColor, Name, string.Format(txt, args));

        public void Msg(Color txt_color, object obj) => _Msg(DefaultMAUSEColor, txt_color, Name, obj.ToString());

        public void Msg(Color txt_color, string txt) => _Msg(DefaultMAUSEColor, txt_color, Name, txt);

        public void Msg(Color txt_color, string txt, params object[] args) => _Msg(DefaultMAUSEColor, txt_color, Name, string.Format(txt, args));

        public void Warning(object obj) => _Warning(Name, obj.ToString());

        public void Warning(string txt) => _Warning(Name, txt);

        public void Warning(string txt, params object[] args) => _Warning(Name, string.Format(txt, args));

        public void Error(object obj) => _Error(Name, obj.ToString());

        public void Error(string txt) => _Error(Name, txt);

        public void Error(string txt, params object[] args) => _Error(Name, string.Format(txt, args));

        public void Error(string txt, Exception ex) => _Error(Name, $"{txt}\n{ex}");

        public void BigError(string txt) => _BigError(Name, txt);

        internal void _Msg(Color extColor, Color textColor, string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext.Pastel(extColor)}]";
            logger.Msg($"{extString} {text.Pastel(textColor)}");
        }

        internal void _Warning(string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext}]";
            logger.Warning($"{extString} {text}");
        }

        internal void _Error(string ext, string text)
        {
            string extString = string.IsNullOrEmpty(ext) ? "" : $"[{ext}]";
            logger.Error($"{extString} {text}");
        }

        internal void _BigError(string ext, string txt)
        {
            _Error(ext, new string('=', 50));
            foreach (var line in txt.Split('\n'))
                _Error(ext, line);
            _Error(ext, new string('=', 50));
        }
    }
}