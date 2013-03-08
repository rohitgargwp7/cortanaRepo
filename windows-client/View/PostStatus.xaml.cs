using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Languages;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using windows_client.DbUtils;

namespace windows_client.View
{
    public partial class PostStatus : PhoneApplicationPage
    {
        private ApplicationBar appBar;
        private ApplicationBarIconButton postStatusIcon;
        public PostStatus()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(PostStatusPage_Loaded);
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            postStatusIcon = new ApplicationBarIconButton();
            postStatusIcon.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            postStatusIcon.Text = AppResources.Conversations_PostStatus_AppBar;
            postStatusIcon.Click += new EventHandler(btnPostStatus_Click);
            postStatusIcon.IsEnabled = true;
            appBar.Buttons.Add(postStatusIcon);
          
            postStatusPage.ApplicationBar = appBar;
        }

        private void btnPostStatus_Click(object sender, EventArgs e)
        {
            postStatusIcon.IsEnabled = false;
            string statusText = txtStatus.Text;
            if (statusText.Trim() == string.Empty)
            {
                postStatusIcon.IsEnabled = true;
                return;
            }
            AccountUtils.postStatus(statusText, postStatus_Callback);
        }

        void PostStatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtStatus.Focus();
            this.Loaded -= PostStatusPage_Loaded;
        }
        public void postStatus_Callback(JObject obj)
        {
            string stat = "";
            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue(HikeConstants.STAT, out statusToken);
                stat = statusToken.ToString();
            }
            if (stat == HikeConstants.OK)
            {
                JToken statusData;
                obj.TryGetValue(HikeConstants.Extras.DATA, out statusData);
                try
                {
                    string statusId = statusData["statusid"].ToString();
                    string message = statusData["msg"].ToString();
                    StatusMessage sm = new StatusMessage(App.MSISDN, message, StatusMessage.StatusType.TEXT_UPDATE, statusId,
                        TimeUtils.getCurrentTimeStamp(),-1);
                    StatusMsgsTable.InsertStatusMsg(sm);
                    App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
                }
                catch
                {
                }

            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            });
        }
    }
}