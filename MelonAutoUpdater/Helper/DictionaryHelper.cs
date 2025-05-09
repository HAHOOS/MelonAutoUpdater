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

        #region Get Keys

        /// <summary>
        /// Get all keys in a list of KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="keyValuePair">List of the KeyValuePairs</param>
        /// <returns>Keys from the list of KeyValuePairs</returns>
        public static TKey[] GetKeys<TKey, TValue>(this List<KeyValuePair<TKey, TValue>> keyValuePair)
        {
            List<TKey> keys = new List<TKey>();
            foreach (var val in keyValuePair)
            {
                keys.Add(val.Key);
            }
            return keys.ToArray();
        }

        /// <summary>
        /// Get all keys in an IEnumerable of KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="keyValuePair">an IEnumerable of the KeyValuePairs</param>
        /// <returns>Keys from the IEnumerable of KeyValuePairs</returns>
        public static TKey[] GetKeys<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePair)
        {
            List<TKey> keys = new List<TKey>();
            foreach (var val in keyValuePair)
            {
                keys.Add(val.Key);
            }
            return keys.ToArray();
        }

        /// <summary>
        /// Get all keys in an array of KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="keyValuePair">Array of the KeyValuePairs</param>
        /// <returns>Keys from the array of KeyValuePairs</returns>
        public static TKey[] GetKeys<TKey, TValue>(this KeyValuePair<TKey, TValue>[] keyValuePair)
        {
            List<TKey> keys = new List<TKey>();
            foreach (var val in keyValuePair)
            {
                keys.Add(val.Key);
            }
            return keys.ToArray();
        }

        #endregion Get Keys

        #region Get Values

        /// <summary>
        /// Get all values in a list of KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="keyValuePair">List of the KeyValuePairs</param>
        /// <returns>Values from the list of KeyValuePairs</returns>
        public static TValue[] GetValues<TKey, TValue>(this List<KeyValuePair<TKey, TValue>> keyValuePair)
        {
            List<TValue> vals = new List<TValue>();
            foreach (var val in keyValuePair)
            {
                vals.Add(val.Value);
            }
            return vals.ToArray();
        }

        /// <summary>
        /// Get all values in an IEnumerable of KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="keyValuePair">an IEnumerable of the KeyValuePairs</param>
        /// <returns>Values from the IEnumerable of KeyValuePairs</returns>
        public static TValue[] GetValues<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePair)
        {
            List<TValue> vals = new List<TValue>();
            foreach (var val in keyValuePair)
            {
                vals.Add(val.Value);
            }
            return vals.ToArray();
        }

        /// <summary>
        /// Get all values in an array of KeyValuePairs
        /// </summary>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <typeparam name="TValue">Type of value</typeparam>
        /// <param name="keyValuePair">Array of the KeyValuePairs</param>
        /// <returns>Values from the array of KeyValuePairs</returns>
        public static TValue[] GetValues<TKey, TValue>(this KeyValuePair<TKey, TValue>[] keyValuePair)
        {
            List<TValue> vals = new List<TValue>();
            foreach (var val in keyValuePair)
            {
                vals.Add(val.Value);
            }
            return vals.ToArray();
        }

        #endregion Get Values
    }
}