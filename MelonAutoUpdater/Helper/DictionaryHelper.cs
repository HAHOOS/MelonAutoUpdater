using System.Collections.Generic;

namespace MelonAutoUpdater.Helper
{
    /// <summary>
    /// Helper class for <see cref="Dictionary{TKey, TValue}"/>
    /// </summary>
    public static class DictionaryHelper
    {
        /// <summary>
        /// Checks if this <see cref="Dictionary{TKey, TValue}"/> contains provided keys
        /// </summary>
        /// <param name="dictionary">Dictionary to check</param>
        /// <param name="keys">Keys to check</param>
        /// <returns><see langword="true"/> if all found, otherwise <see langword="false"/></returns>
        public static bool ContainsKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, params TKey[] keys)
        {
            foreach (TKey key in keys)
            {
                if (!dictionary.ContainsKey(key)) return false;
            }
            return true;
        }
    }
}