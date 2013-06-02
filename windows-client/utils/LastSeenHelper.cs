using Microsoft.Phone.Reactive;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.utils
{
    class LastSeenHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile LastSeenHelper instance = null;
        private readonly int maxRequestCount = 2;
        private int currentRequestCount = 0;
        private string cNumber;

        public void requestLastSeen(string number)
        {
            if (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                cNumber = number;
                AccountUtils.LastSeenRequest(requestLastSeen_Callback, cNumber);
            }
            else
            {
                if (UpdateLastSeen != null)
                    UpdateLastSeen(this, null);

                return;
            }
        }

        public void requestLastSeen_Callback(JObject obj)
        {
            string stat = "";
            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue(HikeConstants.STAT, out statusToken);
                if (statusToken != null)
                    stat = statusToken.ToString();
            }
            if (stat != HikeConstants.OK)
            {
                if (currentRequestCount < maxRequestCount)
                {
                    requestLastSeen(cNumber);
                }
                else
                {
                    if (UpdateLastSeen != null)
                        UpdateLastSeen(this, null);

                    return;
                }

                currentRequestCount++;
            }
            else if (stat == HikeConstants.OK)
            {
                JToken dataToken, lastSeenToken;
                obj.TryGetValue(HikeConstants.DATA, out dataToken);
                var jObj = JObject.Parse(dataToken.ToString());
                jObj.TryGetValue(HikeConstants.LASTSEEN, out lastSeenToken);

                if (lastSeenToken != null && UpdateLastSeen != null)
                    UpdateLastSeen(this, new LastSeenEventArgs() { TimeStamp = Convert.ToInt64(lastSeenToken.ToString()), ContactNumber = cNumber });
            }
        }

        public string GetLastSeenTimeStampStatus(long timeStamp)
        {
            if (timeStamp == 0)
                return Languages.AppResources.Online;

            if (timeStamp == -1)
                return "";

            return "\uE121 " + Languages.AppResources.Last_Seen + " " + TimeUtils.getRelativeTime(timeStamp / 1000);
        }

        readonly DateTime EPOCH_TIME = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public event EventHandler<LastSeenEventArgs> UpdateLastSeen;
    }

    public class LastSeenEventArgs : EventArgs
    {
        public long TimeStamp;
        public string ContactNumber;

    }
}
