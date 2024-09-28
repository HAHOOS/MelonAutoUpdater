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
        /// Checks if collection contains a key
        /// </summary>
        /// <param name="collection">Collection</param>
        /// <param name="key">Key to check</param>
        /// <returns><see langword="true"/> if collection contains the specified key, otherwise <see langword="false"/></returns>
        public static bool Contains(this WebHeaderCollection collection, string key)
        {
            if (collection == null) return false;
            return collection.AllKeys.Contains(key);
        }
    }
}