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

            if (delta < 60)
            {
                return ts.Seconds == 1 ? AppResources.TimeUtils_One_Sec_Ago_Txt : string.Format(AppResources.TimeUtils_X_Secs_Ago_Txt, ts.Seconds);
            }
            if (delta < 120)
            {
                return AppResources.TimeUtils_A_Min_Ago_Txt;
            }
            if (delta < 2700) // 45 * 60
            {
                return string.Format(AppResources.TimeUtils_X_Mins_Ago_Txt, ts.Minutes);
            }
            if (delta < 5400) // 90 * 60
            {
                return AppResources.TimeUtils_An_hour_Ago_Txt;
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

        public static string getRelativeTimeForStatuses(long epochtimestamp)
        {
            DateTime startDateTime = new DateTime(1970, 1, 1);
            TimeSpan t = DateTime.UtcNow - startDateTime;
            int monthsDifference = MonthsDifference(startDateTime, DateTime.UtcNow);
            int yearsDifference = monthsDifference / 12;
            if (yearsDifference > 0)
            {
                if (yearsDifference == 1)
                    return AppResources.TimeUtils_One_Year_Ago_Txt;
                else
                    return string.Format(AppResources.TimeUtils_X_Years_Ago_Txt, yearsDifference);
            }
            else if (monthsDifference > 0)
            {
                if (monthsDifference == 1)
                    return AppResources.TimeUtils_One_Month_Ago_Txt;
                else
                    return string.Format(AppResources.TimeUtils_X_Month_Ago_Txt, monthsDifference);
            }
            else if (t.TotalDays > 0)
            {
                if (t.TotalDays == 1)
                    return AppResources.Yesterday_Txt;
                else
                    return string.Format(AppResources.TimeUtils_X_Days_Ago_Txt, t.TotalDays);
            }
            else if (t.TotalHours > 0)
            {
                if (t.Hours == 1 && t.Minutes < 30)
                    return AppResources.TimeUtils_An_hour_Ago_Txt;
                if (t.Minutes < 30)
                    return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, t.Hours);
                else
                    return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, t.Hours.ToString() + ".5");
            }
            else if (t.Minutes > 5)
            {
                return string.Format(AppResources.TimeUtils_X_Mins_Ago_Txt, t.Minutes);
            }
            else
            {
                return AppResources.TimeUtils_Moments_Ago;
            }
        }

        public static int MonthsDifference(DateTime start, DateTime end)
        {
            return (start.Year * 12 + start.Month) - (end.Year * 12 + end.Month);
        }
    }
}
