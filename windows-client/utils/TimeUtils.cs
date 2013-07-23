using System;
using System.Text;
using windows_client.Languages;
using System.Globalization;

namespace windows_client.utils
{
    public class TimeUtils
    {
        public enum TimeIntervalOfDay
        { 
            MORNING,
            NOON,
            NIGHT
        };

        public static string GetMonthDate(DateTime date)
        {
            string tmp = date.ToString("d").Replace(date.ToString("yyyy"), string.Empty);
            char last = tmp[tmp.Length - 1];
            char[] trimmer = char.IsDigit(last) ? new char[] { tmp[0] } : new char[] { last };
            string dateStr = tmp.Trim(trimmer);
            return dateStr;
        }

        //used on conversation list
        public static string getTimeString(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;

            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now.Subtract(messageTime);
            messageTime = messageTime.ToLocalTime();

            if (span.Days < 1)
                return messageTime.ToShortTimeString();
            else if (span.Days < 7)
                return messageTime.ToString("ddd", CultureInfo.CurrentUICulture);
            else if (span.Days < 365)
                return GetMonthDate(messageTime);
            else
                return messageTime.ToShortDateString();
        }

        public static string getTimeStringForChatThread(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;

            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now.Subtract(messageTime);
            messageTime = messageTime.ToLocalTime();

            if (span.Days < 1)
                return messageTime.ToShortTimeString();
            else if (span.Days < 7)
                return messageTime.ToString("ddd, ", CultureInfo.CurrentUICulture) + messageTime.ToShortTimeString();
            else if (span.Days < 365)
                return String.Format("{0}, {1}", GetMonthDate(messageTime), messageTime.ToShortTimeString());
            else
                return String.Format("{0}, {1}", messageTime.ToShortDateString(), messageTime.ToShortTimeString());
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

        public static string getRelativeTimeForLastSeen(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime receivedTime = new DateTime(ticks);
            receivedTime = receivedTime.ToLocalTime();

            if (receivedTime.Date == DateTime.Now.Date) //today
                return Languages.AppResources.Last_Seen_Today_At + " " + receivedTime.ToShortTimeString();
            else if ((DateTime.Now.Date - receivedTime).Days == 1) // yesterday
                return Languages.AppResources.Last_Seen_Yesterday_At + " " + receivedTime.ToShortTimeString();
            else if ((DateTime.Now.Date - receivedTime).Days < 7) // less than two weeks ago
                return Languages.AppResources.Last_Seen + " " + GetMonthDateTime(receivedTime);
            else
                return Languages.AppResources.Last_Seen + " " + AppResources.TimeUtils_A_While_Ago;
        }

        static string GetMonthDateTime(DateTime time)
        {
            var number = time.Day;

                switch (number % 100)
                {
                    case 21:
                    case 31:
                        return time.ToString("d\\s\\t MMM, ") + time.ToShortTimeString();
                    case 22:
                        return time.ToString("d\\n\\d MMM, ") + time.ToShortTimeString();
                    case 23:
                        return time.ToString("d\\r\\d MMM, ") + time.ToShortTimeString();
                    case 11:
                    case 12:
                    case 13:
                        return time.ToString("d\\t\\h MMM, ") + time.ToShortTimeString();
                }

                switch (number % 10)
                {
                    case 1:
                        return time.ToString("d\\s\\t MMM, ") + time.ToShortTimeString();
                    case 2:
                        return time.ToString("d\\n\\d MMM, ") + time.ToShortTimeString();
                    case 3:
                        return time.ToString("d\\r\\d MMM, ") + time.ToShortTimeString();
                    default:
                        return time.ToString("d\\t\\h MMM, ") + time.ToShortTimeString();
                }
        }

        public static string getRelativeTime(long timestamp)
        {
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Subtract(TimeSpan.FromSeconds(timestamp));
            double delta = ts.TotalSeconds;
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

        public static TimeIntervalOfDay GetTimeIntervalDay()
        {
            if (DateTime.Now.Hour >= 4 && DateTime.Now.Hour < 12)
                return TimeIntervalOfDay.MORNING;
            else if (DateTime.Now.Hour >= 12 && DateTime.Now.Hour < 20)
                return TimeIntervalOfDay.NOON;
            else
                return TimeIntervalOfDay.NIGHT;
        }

        public static string GetOnHikeSinceDisplay(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime messageTime = new DateTime(ticks);
            return messageTime.ToString("MMM yy", CultureInfo.CurrentUICulture);
        }

    }
}
