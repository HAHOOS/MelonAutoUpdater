using System;

namespace MelonAutoUpdater.Helper
{
    /// <summary>
    /// Class with methods mainly helping with using Unix timestamp in Net Framework 3.5
    /// </summary>
    public static class DateTimeOffsetHelper
    {
        /// <summary>
        /// Converts <see cref="DateTimeOffset" /> to Unix timestamp in seconds
        /// </summary>
        /// <param name="_">Ignore</param>
        /// <returns>Unix Timestamp in seconds of <see cref="DateTimeOffset" /></returns>
        public static long ToUnixTimeSeconds(this DateTimeOffset _) => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        /// <summary>
        /// Converts <see cref="DateTimeOffset" /> to Unix timestamp in milliseconds
        /// </summary>
        /// <param name="_">Ignore</param>
        /// <returns>Unix Timestamp in milliseconds of <see cref="DateTimeOffset" /></returns>
        public static long ToUnixTimeMilliseconds(this DateTimeOffset _) => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

        /// <summary>
        /// Converts Unix timestamp in seconds to <see cref="DateTimeOffset" />
        /// </summary>
        /// <param name="seconds">The Unix timestamp in seconds</param>
        /// <returns><see cref="DateTimeOffset" /> with date and time corresponding to Unix timestamp</returns>
        public static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            return (new DateTime(1970, 1, 1)).AddSeconds(seconds).ToLocalTime();
        }

        /// <summary>
        /// Converts Unix timestamp in milliseconds to <see cref="DateTimeOffset" />
        /// </summary>
        /// <param name="milliseconds">The Unix timestamp in milliseconds</param>
        /// <returns><see cref="DateTimeOffset" /> with date and time corresponding to Unix timestamp</returns>
        public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
        {
            return (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds).ToLocalTime();
        }
    }
}