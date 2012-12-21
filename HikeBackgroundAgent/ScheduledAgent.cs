using System.Windows;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Notification;
using System;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;

namespace HikeBackgroundAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static readonly int PRODUCTION_PORT = 80;

        private static readonly int STAGING_PORT = 8080;

        private static readonly string PRODUCTION_HOST = "api.im.hike.in";

        private static readonly string STAGING_HOST = "staging.im.hike.in";

        public static string HOST = IsProd ? PRODUCTION_HOST : STAGING_HOST;

        public static int PORT = IsProd ? PRODUCTION_PORT : STAGING_PORT;

        public static readonly string BACKGROUND_AGENT_FILE = "token";
        public static readonly string BACKGROUND_AGENT_DIRECTORY = "ba";


        public static bool IsProd
        {
            get
            {
                return true;
            }
        }


        public static readonly string BASE = "http://" + HOST + ":" + Convert.ToString(PORT) + "/v1";


        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            //TODO: Add code to perform your task in background
            HttpNotificationChannel pushChannel = HttpNotificationChannel.Find("Whatsapp");
            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel("HikeApp");

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.BindToShellTile();
                pushChannel.BindToShellToast();
            }
            else
            {
                NotifyComplete();
            }
        }
        private string readUserToken()
        {
            string token;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
            {
                string FileName = BACKGROUND_AGENT_DIRECTORY + "/" + BACKGROUND_AGENT_FILE;

                if (!store.FileExists(FileName))
                    return null;

                using (var file = store.OpenFile(FileName, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(file))
                    {
                        token = reader.ReadString();
                    }
                }
            }
            return token;

        }
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            string userToken = readUserToken();
            if (!String.IsNullOrEmpty(userToken))
            {
                HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/device")) as HttpWebRequest;
                req.Headers["Cookie"] = "user=" + userToken;
                req.Method = "POST";
                req.ContentType = "application/json";
                req.BeginGetRequestStream(setParams_Callback, new object[] { req, e.ChannelUri.ToString() });
            }
            else
            {
                NotifyComplete();
            }
        }

        private void setParams_Callback(IAsyncResult result)
        {
            JObject data = new JObject();
            object[] vars = (object[])result.AsyncState;
            HttpWebRequest req = vars[0] as HttpWebRequest;
            Stream postStream = req.EndGetRequestStream(result);
            string uri = (string)vars[1];
            data.Add("dev_token", uri);
            data.Add("dev_type", "windows");

            using (StreamWriter sw = new StreamWriter(postStream))
            {
                string json = data.ToString(Newtonsoft.Json.Formatting.None);
                sw.Write(json);
            }
            postStream.Close();
            req.BeginGetResponse(json_Callback, new object[] { req });
        }

        private void json_Callback(IAsyncResult result)
        {
            NotifyComplete();
        }
    }
}