using System.Linq;
using System.Text.RegularExpressions;

namespace MelonAutoUpdater.Utils
{
    /// <summary>
    /// Class for easier use of ANSI
    /// </summary>
    public static class ANSI
    {
        private const string _formatStringStart = "\u001b[{0}m";
        private const string _formatStringContent = "{1}";
        private const string _formatStringEnd = "\u001b[0m";
        private static readonly string _formatStringFull = $"{_formatStringStart}{_formatStringContent}{_formatStringEnd}";

        /// <summary>
        /// Inserts provided ANSI codes into the text
        /// </summary>
        /// <param name="text">The text u want to insert the ANSI codes to</param>
        /// <param name="codes">The ANSI codes</param>
        /// <returns>String with ANSI codes inserted</returns>
        public static string InsertANSI(this string text, params int[] codes)
        {
            if (codes == null || codes.Length == 0 || string.IsNullOrEmpty(text)) return text;
            string code = string.Empty;
            if (codes.Length > 1) codes.ToList().ForEach(x => { if (string.IsNullOrEmpty(code)) code = x.ToString(); else code = $"{code};{x}"; });
            else code = codes.First().ToString();
            string ansi = text.ContainsANSI();
            if (string.IsNullOrEmpty(ansi)) return string.Format(_formatStringFull, code, text);
            else
            {
                if (text.StartsWith(ansi) && text.EndsWith("\u001b[0m"))
                {
                    string newAnsi = ansi.Remove(ansi.Length - 1);
                    newAnsi = $"{newAnsi};{code}m";
                    return text.Replace(ansi, newAnsi);
                }
                else
                {
                    return string.Format(_formatStringFull, code, text);
                }
            }
        }

        #region Decorations

        /// <summary>
        /// Bold provided text with ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to bold</param>
        /// <returns>Bold text</returns>
        public static string Bold(this string text)
        {
            return InsertANSI(text, 1);
        }

        /// <summary>
        /// Dim provided text with ANSI characters
        /// </summary>
        /// <param name="text">The text u want to dim</param>
        /// <returns>Dimmed text</returns>
        public static string Dim(this string text)
        {
            return InsertANSI(text, 2);
        }

        /// <summary>
        /// Underline provided text with ANSI characters
        /// </summary>
        /// <param name="text">The text u want to underline</param>
        /// <returns>Underlined text</returns>
        public static string Underline(this string text)
        {
            return InsertANSI(text, 4);
        }

        /// <summary>
        /// Blink provided text with ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to blink</param>
        /// <returns>Blinking text</returns>
        public static string Blink(this string text)
        {
            return InsertANSI(text, 5);
        }

        /// <summary>
        /// Reverse the colors of the provided text with ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to reverse</param>
        /// <returns>Reversed text</returns>
        public static string Reverse(this string text)
        {
            return InsertANSI(text, 7);
        }

        #endregion Decorations

        #region Utilities

        /// <summary>
        /// Removes ANSI escape characters from provided text
        /// </summary>
        /// <param name="text">The text u want to remove ANSI escape characters from</param>
        /// <returns>Text without ANSI escape characters</returns>
        public static string RemoveANSI(this string text)
        {
            string _txt = Regex.Replace(text, @"(\x1B|\e|\033)\[(.*?)m", "");
            return _txt;
        }

        /// <summary>
        /// Checks for ANSI escape characters in provided text
        /// </summary>
        /// <param name="text">The text u want to check for ANSI escape characters in</param>
        /// <returns>If returned <see langword="string"/> is not <see langword="null"/>, text contains ANSI escape characters. Returns the first ANSI escape characters found</returns>
        public static string ContainsANSI(this string text)
        {
            var _txt = Regex.Match(text, @"(\x1B|\e|\033)\[(.*?)m");
            return (_txt.Success && _txt.Groups.Count >= 3) ? _txt.Groups[0].Value : null;
        }

        #endregion Utilities
    }
}