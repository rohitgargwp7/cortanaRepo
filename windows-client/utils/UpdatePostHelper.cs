using Microsoft.Phone.Reactive;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.utils
{
    class UpdatePostHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile UpdatePostHelper instance = null;
        private readonly int maxPollingTime = 120;
        private volatile int pollingTime; //in seconds
        private readonly int minPollingTime = 3;
        private volatile IScheduler scheduler; //TODO MG - we should can try pooling of scheduler objects
        private volatile IDisposable httpPostScheduled;

        //TODO - MG - Merge this with PushHelper. Create a generic class for handling post requests with retry support
        //using singleton, as there as some changes in .Net 4.0 related to instantiation of static variables.
        //minimal changes to make things work.
        public static UpdatePostHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new UpdatePostHelper();
                        }
                    }
                }
                return instance;
            }
        }

        private UpdatePostHelper()
        {
        }

        public void postAppInfo()
        {
            AccountUtils.postUpdateInfo(postUpdateInfo_Callback);
        }

        public void postUpdateInfo_Callback(JObject obj)
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
                if (scheduler == null)
                {
                    scheduler = Scheduler.NewThread;
                }
                httpPostScheduled = scheduler.Schedule(postAppInfo, TimeSpan.FromSeconds(pollingTime));
                pollingTime *= 2;
                if (pollingTime > maxPollingTime)
                    pollingTime = minPollingTime;
            }
            else if (stat == HikeConstants.OK)
            {
                App.RemoveKeyFromAppSettings(App.APP_UPDATE_POSTPENDING);
                if (httpPostScheduled != null)
                {
                    httpPostScheduled.Dispose();
                    httpPostScheduled = null;
                }
                scheduler = null;
            }
        }
    }
}
