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
        private static readonly string _formatStringFull = $"{_formatStringStart}{_formatStringContent}{Reset}";

        /// <summary>
        /// ANSI escape character to reset all styles
        /// </summary>
        public static readonly string Reset = "\u001b[0m";

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
        /// Make provided text italic with ANSI characters
        /// </summary>
        /// <param name="text">The text u want to make italic</param>
        /// <returns>Italic text</returns>
        public static string Italic(this string text)
        {
            return InsertANSI(text, 4);
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

        /// <summary>
        /// Make provided text invisible with ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to make invisible</param>
        /// <returns>Invisible text</returns>
        public static string Invisible(this string text)
        {
            return InsertANSI(text, 8);
        }

        /// <summary>
        /// Strike-through provided text with ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to strike-through</param>
        /// <returns>Strike-through text</returns>
        public static string StrikeThrough(this string text)
        {
            return InsertANSI(text, 9);
        }

        /// <summary>
        /// Double underline provided text with ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to double underline</param>
        /// <returns>Doubly-underlined text</returns>
        public static string DoubleUnderline(this string text)
        {
            return InsertANSI(text, 21);
        }

        /// <summary>
        /// Makes provided text neither bold or dim using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to normalize</param>
        /// <returns>Neither bold or dim text</returns>
        public static string Normal(this string text)
        {
            return InsertANSI(text, 22);
        }

        /// <summary>
        /// If provided text is italic, removes italic using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to "un-italic"</param>
        /// <returns>Not italic text</returns>
        public static string UnItalic(this string text)
        {
            return InsertANSI(text, 23);
        }

        /// <summary>
        /// If provided text is underlined, removes underline using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to "un-underline"</param>
        /// <returns>Not underlined text</returns>
        public static string UnUnderline(this string text)
        {
            return InsertANSI(text, 24);
        }

        /// <summary>
        /// If provided text is blinking, removes the blinking effect using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to steady</param>
        /// <returns>Not blinking text</returns>
        public static string Steady(this string text)
        {
            return InsertANSI(text, 25);
        }

        /// <summary>
        /// If provided text is reversed, removes the reverse effect using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to positive, aka "un-reverse"</param>
        /// <returns>Not reversed text</returns>
        public static string Positive(this string text)
        {
            return InsertANSI(text, 27);
        }

        /// <summary>
        /// If provided text is invisible, make it visible using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to make visible</param>
        /// <returns>Visible text</returns>
        public static string Visible(this string text)
        {
            return InsertANSI(text, 28);
        }

        /// <summary>
        /// If provided text is strike-through, make it not strike-through using ANSI escape characters
        /// </summary>
        /// <param name="text">The text u want to make "un-strike-through"</param>
        /// <returns>Not strike-through text</returns>
        public static string UnStrikeThrough(this string text)
        {
            return InsertANSI(text, 29);
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

        /// <summary>
        /// Inserts provided ANSI escape characters into the text
        /// </summary>
        /// <param name="text">The text u want to insert the ANSI escape characters to</param>
        /// <param name="codes">The ANSI escape characters</param>
        /// <returns>String with ANSI escape characters inserted</returns>
        public static string InsertANSI(this string text, params int[] codes)
        {
            if (codes == null || codes.Length == 0 || string.IsNullOrEmpty(text)) return text;
            string code = string.Empty;
            if (codes.Length > 1) codes.ToList().ForEach(x => { if (string.IsNullOrEmpty(code)) code = x.ToString(); else code = $"{code};{x}"; });
            else code = codes[0].ToString();
            string ansi = text.ContainsANSI();
            if (string.IsNullOrEmpty(ansi))
            {
                return string.Format(_formatStringFull, code, text);
            }
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

        #endregion Utilities
    }
}