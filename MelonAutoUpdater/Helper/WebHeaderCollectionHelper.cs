using System.Linq;
using System.Net;

namespace MelonAutoUpdater.Helper
{
    /// <summary>
    /// Helper for WebHeaderCollection
    /// </summary>
    public static class WebHeaderCollectionHelper
    {
        /// <summary>
        /// Checks if collection contains a key.
        /// </summary>
        /// <param name="collection">Collection</param>
        /// <param name="key">Key to check</param>
        /// <param name="caseSensitive">If true, will check casing as well</param>
        /// <returns><see langword="true"/> if collection contains the specified key, otherwise <see langword="false"/></returns>
        public static bool Contains(this WebHeaderCollection collection, string key, bool caseSensitive = true)
        {
            if (collection == null) return false;
            return caseSensitive ? collection.AllKeys.Contains(key) : collection.AllKeys.Where(x => x.ToLower() == key.ToLower()).Any();
        }
    }
}