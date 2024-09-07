using System;

namespace MelonAutoUpdater.Helper
{
    public static class DateTimeOffsetHelper
    {
        public static long ToUnixTimeSeconds(this DateTimeOffset dateTimeOffset) => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        public static long ToUnixTimeMilliSeconds(this DateTimeOffset dateTimeOffset) => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

        public static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            return (new DateTime(1970, 1, 1)).AddSeconds(seconds).ToLocalTime();
        }

        public static DateTimeOffset FromUnixTimeMilliSeconds(long milliseconds)
        {
            return (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds).ToLocalTime();
        }
    }
}