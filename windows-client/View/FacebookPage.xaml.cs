using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Facebook;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using windows_client.utils;
using Newtonsoft.Json.Linq;

namespace windows_client.View
{
    public partial class FacebookPage : PhoneApplicationPage
    {
        private const string extendedPermissions = "user_about_me,publish_stream";
        private readonly FacebookClient _fb = new FacebookClient();

        public FacebookPage()
        {
            InitializeComponent();
            if (App.appSettings.Contains("FbLoggedIn"))
                Logout();
            else
                LogIn();
        }

        private void LogIn()
        {
            var parameters = new Dictionary<string, object>();
            parameters["client_id"] = Misc.Social.FacebookSettings.AppID;
            parameters["redirect_uri"] = "https://www.facebook.com/connect/login_success.html";
            parameters["response_type"] = "token";
            parameters["display"] = "touch";
            parameters["scope"] = extendedPermissions;
            BrowserControl.Navigate(_fb.GetLoginUrl(parameters));
        }

        private void Logout()
        {
            var fb = new FacebookClient();
            var parameters = new Dictionary<string, object>();
            parameters["access_token"] = (string)App.appSettings["FbAccessToken"];
            parameters["next"] = "https://www.facebook.com/connect/login_success.html";
            var logoutUrl = fb.GetLogoutUrl(parameters);
            BrowserControl.Navigate(logoutUrl);
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            FacebookOAuthResult oauthResult;
            if (e.Uri.AbsoluteUri == "https://www.facebook.com/connect/login_success.html")
            {
                App.RemoveKeyFromAppSettings(HikeConstants.FB_LOGGED_IN);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NavigationService.GoBack();
                    AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(Invite.SocialDeleteFB), "fb", false);
                });
            }
            if (!_fb.TryParseOAuthCallbackUrl(e.Uri, out oauthResult))
            {
                return;
            }

            if (oauthResult.IsSuccess)
            {
                var accessToken = oauthResult.AccessToken;
                LoginSucceded(accessToken);
            }
            else
            {
                MessageBox.Show(oauthResult.ErrorDescription);
                NavigationService.GoBack(); // take you to the proper page
            }
        }
        private void LoginSucceded(string accessToken)
        {
            var fb = new FacebookClient(accessToken);

            fb.GetCompleted += (o, e) =>
            {
                if (e.Error != null)
                {
                    Dispatcher.BeginInvoke(() => 
                        {
                            MessageBox.Show(e.Error.Message);
                            NavigationService.GoBack();
                        });
                    return;
                }

                var result = (IDictionary<string, object>)e.GetResultData();
                string id = (string)result["id"];
                App.WriteToIsoStorageSettings("FbAccessToken", accessToken);
                App.WriteToIsoStorageSettings("FbUserId", id);
                App.WriteToIsoStorageSettings(HikeConstants.FB_LOGGED_IN, true);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    JObject oj = new JObject();
                    oj["id"] = id;
                    oj["token"] = accessToken;
                    NavigationService.GoBack();
                    AccountUtils.SocialPost(oj, new AccountUtils.postResponseFunction(Invite.SocialPostFB), "fb", true);
                });               
            };
            fb.GetAsync("me?fields=id");
        }
    }
}