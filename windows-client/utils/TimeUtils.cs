﻿using System;
using System.Text;

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
                messageTimeString.Append(String.Format("{0:00}",(messageTime.Hour % 12))).Append(":").Append(String.Format("{0:00}",(messageTime.Minute))).Append((messageTime.Hour / 12) == 0 ? "a" : "p");
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
                messageTimeString.Append(messageTime.Day).Append("/").Append(messageTime.Month).Append("/").Append(messageTime.Year%100);
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
//            TimeSpan.FromMilliseconds(milliseconds);
            long timespanMilliSeconds = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds - timestamp * 1000;
            TimeSpan ts = TimeSpan.FromMilliseconds(timespanMilliSeconds );
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 60)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }
            if (delta < 120)
            {
                return "a minute ago";
            }
            if (delta < 2700) // 45 * 60
            {
                return ts.Minutes + " minutes ago";
            }
            if (delta < 5400) // 90 * 60
            {
                return "an hour ago";
            }
            if (delta < 86400) // 24 * 60 * 60
            {
                return ts.Hours + " hours ago";
            }
            if (delta < 172800) // 48 * 60 * 60
            {
                return "yesterday";
            }
            if (delta < 2592000) // 30 * 24 * 60 * 60
            {
                return ts.Days + " days ago";
            }
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }

        public static long getCurrentTimeStamp()
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds
            return ticks;        
        }
    }
}
