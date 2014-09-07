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

        //Used on video preview: Function to get duration in Human Readable form from Milliseconds
        public static string GetDurationInHourMinFromMilliseconds(int ms)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            string answer = t.TotalHours >= 1 ? t.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture)
                                             : t.ToString("mm\\:ss", CultureInfo.InvariantCulture);
            return answer;
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
                return messageTime.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
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
                return messageTime.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
            else if (span.Days < 7)
                return messageTime.ToString("ddd, ", CultureInfo.CurrentUICulture) + messageTime.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
            else if (span.Days < 365)
                return String.Format("{0}, {1}", GetMonthDate(messageTime), messageTime.ToShortTimeString().Replace(" AM","a").Replace(" PM","p"));
            else
                return String.Format("{0}, {1}", messageTime.ToShortDateString(), messageTime.ToShortTimeString().Replace(" AM","a").Replace(" PM","p"));
        }

        public static string getTimeStringForEmailConversation(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;

            DateTime messageTime = new DateTime(ticks);
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now.Subtract(messageTime);
            messageTime = messageTime.ToLocalTime();

            return String.Format("{0}, {1}", messageTime.ToShortDateString(), messageTime.ToShortTimeString().Replace(" AM", "a").Replace(" PM", "p"));
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
                return String.Format(Languages.AppResources.Last_Seen_Today_At, receivedTime.ToShortTimeString().Replace(" AM", "a").Replace(" PM", "p"));
            else if ((DateTime.Now.Date - receivedTime).Days <= 1) // yesterday
                return String.Format(Languages.AppResources.Last_Seen_Yesterday_At, receivedTime.ToShortTimeString().Replace(" AM", "a").Replace(" PM", "p"));
            else if ((DateTime.Now.Date - receivedTime).Days < 8) // less than one week ago
                return String.Format(Languages.AppResources.Last_Seen, GetMonthDateTime(receivedTime));
            else
                return String.Format(Languages.AppResources.Last_Seen, AppResources.TimeUtils_A_While_Ago);
        }

        static string GetMonthDateTime(DateTime time)
        {
            var number = time.Day;

                switch (number % 100)
                {
                    case 21:
                    case 31:
                        return time.ToString("d\\s\\t MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                    case 22:
                        return time.ToString("d\\n\\d MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                    case 23:
                        return time.ToString("d\\r\\d MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                    case 11:
                    case 12:
                    case 13:
                        return time.ToString("d\\t\\h MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                }

                switch (number % 10)
                {
                    case 1:
                        return time.ToString("d\\s\\t MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                    case 2:
                        return time.ToString("d\\n\\d MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                    case 3:
                        return time.ToString("d\\r\\d MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                    default:
                        return time.ToString("d\\t\\h MMM, ") + time.ToShortTimeString().Replace(" AM","a").Replace(" PM","p");
                }
        }

        public static string getRelativeTime(long timestamp)
        {
            long ticks = timestamp * 10000000;
            ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
            DateTime receivedTime = new DateTime(ticks);
            receivedTime = receivedTime.ToLocalTime();
            TimeSpan ts = DateTime.Now.Subtract(receivedTime);

            if (ts.TotalMinutes < 5)
                return AppResources.TimeUtils_Moments_Ago;
            
            if (ts.TotalMinutes < 60) //60 * 60
                return string.Format(AppResources.TimeUtils_X_Mins_Ago_Txt, ts.Minutes);
            
            if (ts.TotalHours < 1.5) // 1.5 * 60 * 60
                return AppResources.TimeUtils_An_hour_Ago_Txt;
            
            if (ts.TotalHours < 3) //3 * 60 * 60
            {
                int minuteOfHour = ts.Minutes % 60;
                if (minuteOfHour < 30)
                    return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, ts.Hours);
                return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, ts.Hours.ToString() + ".5");
            }
        
            if (ts.TotalHours < 24) // 24 * 60 * 60
                return string.Format(AppResources.TimeUtils_X_hours_Ago_Txt, ts.Hours);

            var days = (DateTime.Now.Date - receivedTime.Date).Days;
            
            if (receivedTime.Date != DateTime.Now.Date && days <= 1)
                return AppResources.TimeUtils_1Day_Ago_Txt;
            
            if (days < 30) // 30 * 24 * 60 * 60
                return string.Format(AppResources.TimeUtils_X_Days_Ago_Txt, days);
            
            if (days < 365) // 12 * 30 * 24 * 60 * 60
            {
                int months = days / 30;
                return months <= 1 ? AppResources.TimeUtils_One_Month_Ago_Txt : string.Format(AppResources.TimeUtils_X_Month_Ago_Txt, months);
            }
            
            int years = days / 365;
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
            return messageTime.ToString("MMM yyyy", CultureInfo.CurrentUICulture);
        }

    }
}
