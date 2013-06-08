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
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("h\\:mm tt", CultureInfo.CurrentUICulture);

                messageTimeString.Append(String.Format("{0:00}", (messageTime.Hour % 12))).Append(":").Append(String.Format("{0:00}", (messageTime.Minute))).Append((messageTime.Hour / 12) == 0 ? "a" : "p");
                return messageTimeString.ToString();
            }
            else if (span.Days < 7)
            {
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("ddd HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("ddd h\\:mm tt", CultureInfo.CurrentUICulture);
                return messageTime.DayOfWeek.ToString().Substring(0, 3);
            }//TODO count no of days in that year
            else if (span.Days < 365)
            {
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("d/MMM HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("d/MMM h\\:mm tt", CultureInfo.CurrentUICulture);
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month);
                return messageTimeString.ToString();
            }
            else
            {
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("d/MMM/yy HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("d/MMM/yy h\\:mm tt", CultureInfo.CurrentUICulture);
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
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("h\\:mm tt", CultureInfo.CurrentUICulture);
                messageTimeString.Append(String.Format("{0:00}", (messageTime.Hour % 12))).Append(":").Append(String.Format("{0:00}", (messageTime.Minute))).Append((messageTime.Hour / 12) == 0 ? "a" : "p");
                return messageTimeString.ToString();
            }
            else if (span.Days < 7)
            {
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("ddd HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("ddd h\\:mm tt", CultureInfo.CurrentUICulture);
                messageTimeString.Append(messageTime.DayOfWeek.ToString().Substring(0, 3));
            }//TODO count no of days in that year
            else if (span.Days < 365)
            {
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("d/M HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("d/M h\\:mm tt", CultureInfo.CurrentUICulture);
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month);
            }
            else
            {
                //if (App.Is24HourTimeFormat)
                //    return messageTime.ToString("d/M/yyyy HH\\:mm", CultureInfo.CurrentUICulture);
                //else
                //    return messageTime.ToString("d/M/yyyy h\\:mm tt", CultureInfo.CurrentUICulture);
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

        public static string getRelativeTimeForLastSeen(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime receivedTime = new DateTime(ticks);
            receivedTime = receivedTime.ToLocalTime();

            if (receivedTime.Date == DateTime.Now.Date) //today
            {
                if (App.Is24HourTimeFormat)
                    return Languages.AppResources.Last_Seen + " " + Languages.AppResources.Today_Txt + " " + Languages.AppResources.At_Txt + " " + receivedTime.ToString("HH\\:mm", CultureInfo.CurrentUICulture);
                else
                    return Languages.AppResources.Last_Seen + " " + Languages.AppResources.Today_Txt + " " + Languages.AppResources.At_Txt + " " + receivedTime.ToString("h\\:mm tt", CultureInfo.CurrentUICulture).Replace(" AM", "a").Replace(" PM", "p");
            }
            else if ((receivedTime - DateTime.Now.Date).Days  == 1) // yesterday
            {
                if (App.Is24HourTimeFormat)
                    return Languages.AppResources.Last_Seen + " " + Languages.AppResources.Yesterday_Txt + " " + Languages.AppResources.At_Txt + " " + receivedTime.ToString("HH\\:mm", CultureInfo.CurrentUICulture);
                else
                    return Languages.AppResources.Last_Seen + " " + Languages.AppResources.Yesterday_Txt + " " + Languages.AppResources.At_Txt + " " + receivedTime.ToString("h\\:mm tt", CultureInfo.CurrentUICulture).Replace(" AM", "a").Replace(" PM", "p");
            }
            else if ((receivedTime - DateTime.Now.Date).Days < 7) // less than two weeks ago
            {
                if(App.Is24HourTimeFormat)
                    return Languages.AppResources.Last_Seen + " " + receivedTime.ToString("HH\\:mm, d/M/yyyy", CultureInfo.CurrentUICulture);
                else
                    return Languages.AppResources.Last_Seen + " " + receivedTime.ToString("h\\:mm tt, d/M/yyyy", CultureInfo.CurrentUICulture).Replace(" AM","a").Replace(" PM","p");
            }
            else
                return Languages.AppResources.Last_Seen + " " + AppResources.TimeUtils_A_While_Ago;
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
            return messageTime.ToString("MMM yy");
        }

    }
}
