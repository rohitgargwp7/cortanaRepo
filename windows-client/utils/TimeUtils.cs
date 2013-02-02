using System;
using System.Text;
using windows_client.Languages;

namespace windows_client.utils
{
    public class TimeUtils
    {
        //used on conversation list
        public static string getTimeString(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;

            TimeSpan span = now.Subtract(messageTime);

            messageTime = messageTime.ToLocalTime();

            StringBuilder messageTimeString = new StringBuilder();
            if (span.Days < 1)
            {
                messageTimeString.Append(String.Format("{0:00}", (messageTime.Hour % 12))).Append(":").Append(String.Format("{0:00}", (messageTime.Minute))).Append((messageTime.Hour / 12) == 0 ? "a" : "p");
                return messageTimeString.ToString();
            }
            else if (span.Days < 7)
            {
                return messageTime.DayOfWeek.ToString().Substring(0, 3);
            }//TODO count no of days in that year
            else if (span.Days < 365)
            {
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month);
                return messageTimeString.ToString();
            }
            else
            {
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month).Append("/").Append(messageTime.Year % 100);
                return messageTimeString.ToString();
            }
        }

        public static string getTimeStringForChatThread(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now.Subtract(messageTime);
            messageTime = messageTime.ToLocalTime();
            StringBuilder messageTimeString = new StringBuilder();
            if (span.Days < 1)
            {
                messageTimeString.Append(String.Format("{0:00}", (messageTime.Hour % 12))).Append(":").Append(String.Format("{0:00}", (messageTime.Minute))).Append((messageTime.Hour / 12) == 0 ? "a" : "p");
                return messageTimeString.ToString();
            }
            else if (span.Days < 7)
            {
                messageTimeString.Append(messageTime.DayOfWeek.ToString().Substring(0, 3));
            }//TODO count no of days in that year
            else if (span.Days < 365)
            {
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month);
            }
            else
            {
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month).Append("/").Append(messageTime.Year % 100);
            }
            messageTimeString.Append(", ").Append(String.Format("{0:00}", (messageTime.Hour % 12))).Append(":").Append(String.Format("{0:00}", (messageTime.Minute))).Append((messageTime.Hour / 12) == 0 ? "a" : "p");
            return messageTimeString.ToString();
        }


        public static bool isUpdateTimeElapsed(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now.Subtract(messageTime);
            if (AccountUtils.IsProd)
                return span.Hours > HikeConstants.CHECK_FOR_UPDATE_TIME;
            else
                return span.Minutes > HikeConstants.CHECK_FOR_UPDATE_TIME;
        }

        public static bool isAnalyticsTimeElapsed(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now.Subtract(messageTime);
            if (AccountUtils.IsProd)
                return span.Hours > HikeConstants.ANALYTICS_POST_TIME;
            else
                return span.Minutes > HikeConstants.ANALYTICS_POST_TIME;
        }

        public static string getRelativeTime(long timestamp)
        {
            long timespanMilliSeconds = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - timestamp * 1000;
            TimeSpan ts = TimeSpan.FromMilliseconds(timespanMilliSeconds);
            double delta = Math.Abs(ts.TotalSeconds);
            if (delta < 300) // 60 * 5 
            {
                return AppResources.TimeUtils_Moments_Ago;
            }
            if (delta < 3600) //60 * 60
            {
                return string.Format(AppResources.TimeUtils_X_Mins_Ago_Txt, ts.Minutes);
            }
            if (delta < 5400) // 1.5 * 60 * 60
            {
                return AppResources.TimeUtils_An_hour_Ago_Txt;
            }
            if (delta < 10800) //3 * 60 * 60
            {
                int minuteOfHour = ts.Minutes % 60;
                if (minuteOfHour < 30)
                    return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, ts.Hours);
                return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, ts.Hours.ToString() + ".5");
            }
            if (delta < 86400) // 24 * 60 * 60
            {
                return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, ts.Hours);
            }
            if (delta < 172800) // 48 * 60 * 60
            {
                return AppResources.Yesterday_Txt;
            }
            if (delta < 2592000) // 30 * 24 * 60 * 60
            {
                return string.Format(AppResources.TimeUtils_X_Days_Ago_Txt, ts.Days);
            }
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? AppResources.TimeUtils_One_Month_Ago_Txt : string.Format(AppResources.TimeUtils_X_Month_Ago_Txt, months);
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? AppResources.TimeUtils_One_Year_Ago_Txt : string.Format(AppResources.TimeUtils_X_Years_Ago_Txt, years);
        }

        public static long getCurrentTimeStamp()
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

        public static int MonthsDifference(DateTime start, DateTime end)
        {
            return (start.Year * 12 + start.Month) - (end.Year * 12 + end.Month);
        }
    }
}
