using System;
using System.Text;
using CommonLibrary.Languages;
using System.Globalization;

namespace CommonLibrary.utils
{
    public class TimeUtils
    {
        public static long GetCurrentTimeStamp()
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds
            return ticks;
        }

        public static long getCurrentTimeTicks()
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000; //presicion increased to avoid conflicts in timestamps of failed messages
            return ticks;
        }
    }
}
