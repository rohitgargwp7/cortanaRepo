using Microsoft.Phone.Reactive;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.utils
{
    class HttpPostHelper
    {
        private List<PostInfo> httpPostList = new List<PostInfo>();
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile HttpPostHelper instance = null;
        private volatile static IScheduler scheduler; //TODO MG - we should can try pooling of scheduler objects


        public static HttpPostHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new HttpPostHelper();
                        }
                    }
                }
                return instance;
            }
        }

        private HttpPostHelper()
        {
        }

        public void postToServer(string stringToPost, string relativeUrl)
        {
            PostInfo postInfo = new PostInfo(stringToPost, relativeUrl);
            httpPostList.Add(postInfo);
            AccountUtils.httpPost(postInfo.StringToPost, postInfo.RelativeUrl, postPushNotification_Callback, postInfo);
        }

        public void postPushNotification_Callback(JObject obj, object metadata)
        {
        }

        private class PostInfo
        {
            string _stringToPost;
            string _relativeUrl;
            int retryTime = 5;

            public string StringToPost
            {
                get
                {
                    return _stringToPost;
                }
            }

            public string RelativeUrl
            {
                get
                {
                    return _relativeUrl;
                }
            }

            public PostInfo(string stringToPost, string relativeUrl)
            {
                _stringToPost = stringToPost;
                _relativeUrl = relativeUrl;
            }
        };

    }
}
