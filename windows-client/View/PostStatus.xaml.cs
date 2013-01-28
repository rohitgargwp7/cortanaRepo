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
        public PostStatus()
        {
            InitializeComponent();
            txtStatus.Focus();
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            ApplicationBarIconButton postStatusIcon = new ApplicationBarIconButton();
            postStatusIcon.IconUri = new Uri("/View/images/icon_status.png", UriKind.Relative);
            postStatusIcon.Text = AppResources.AppBar_Next_Btn;
            postStatusIcon.Click += new EventHandler(btnPostStatus_Click);
            postStatusIcon.IsEnabled = true;
            appBar.Buttons.Add(postStatusIcon);
            postStatusPage.ApplicationBar = appBar;
        }

        private void btnPostStatus_Click(object sender, EventArgs e)
        {
            string statusText = txtStatus.Text;
            AccountUtils.postStatus(statusText, postStatus_Callback);
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
                    string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
                    string statusId = statusData["statusid"].ToString();
                    string message = statusData["msg"].ToString();
                    StatusMessage sm = new StatusMessage(msisdn, message, StatusMessage.StatusType.TEXT_UPDATE, statusId,
                        TimeUtils.getCurrentTimeStamp());
                    StatusMsgsTable.InsertStatusMsg(sm);
                }
                catch
                {
                }

            }
        }
    }
}