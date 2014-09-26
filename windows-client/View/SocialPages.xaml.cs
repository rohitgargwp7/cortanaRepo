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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using windows_client.Languages;
using System.Diagnostics;
using windows_client.DbUtils;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using Hammock.Web;
using Hammock.Authentication.OAuth;

namespace windows_client.View
{
    public partial class SocialPages : PhoneApplicationPage
    {
        private string socialNetwork;
        private string tokenSecret = string.Empty;
        private bool fromEnterName;
        string oAuthTokenKey = string.Empty;
        string accessToken = string.Empty;
        string accessTokenSecret = string.Empty;
        string userID = string.Empty;
        private const string extendedPermissionsEnterName = "user_about_me";
        private const string extendedPermissions = "user_about_me,publish_stream";
        private readonly FacebookClient _fb = new FacebookClient();

        public SocialPages()
        {
            InitializeComponent();
            object sn;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.SOCIAL, out sn))
                socialNetwork = (string)sn;

            if (socialNetwork == HikeConstants.ServerJsonKeys.TWITTER)
            {
                PhoneApplicationService.Current.State[HikeConstants.FROM_SOCIAL_PAGE] = true;
                BrowserControl.IsScriptEnabled = false;
                AuthenticateTwitter();
            }
            else if (socialNetwork == HikeConstants.ServerJsonKeys.FACEBOOK)
            {
                PhoneApplicationService.Current.State[HikeConstants.FROM_SOCIAL_PAGE] = true;
                if (PhoneApplicationService.Current.State.ContainsKey("fromEnterName"))
                    fromEnterName = true;
                if (HikeInstantiation.AppSettings.Contains(HikeConstants.FB_LOGGED_IN))
                    LogoutFb();
                else
                    LogInFb();
            }
            else
            {
                //  string url = (AccountUtils.IsProd ? "http://hike.in/" : "http://staging.im.hike.in:8080/") + "rewards/wp7/" + (string)HikeInstantiation.appSettings[HikeConstants.REWARDS_TOKEN];
                string url = "http://" + AccountUtils.HOST + ":" + AccountUtils.PORT + "/rewards/wp7/" + (string)HikeInstantiation.AppSettings[HikeConstants.ServerJsonKeys.REWARDS_TOKEN];
                Uri page = new Uri(url);
                BrowserControl.Navigate(page);
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL);
        }

        private void LogInFb()
        {
            string perms;
            if (fromEnterName)
                perms = extendedPermissionsEnterName;
            else
                perms = extendedPermissions;
            var parameters = new Dictionary<string, object>();
            parameters["client_id"] = Misc.Social.FacebookSettings.AppID;
            parameters["redirect_uri"] = "https://m.facebook.com/connect/login_success.html";
            parameters["response_type"] = "token";
            parameters["display"] = "touch";
            parameters["scope"] = perms;
            BrowserControl.Navigate(_fb.GetLoginUrl(parameters));
        }

        private async void LogoutFb()
        {
            await BrowserControl.ClearCookiesAsync();

            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.FB_ACCESS_TOKEN);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.FB_USER_ID);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.FB_LOGGED_IN);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                PhoneApplicationService.Current.State[HikeConstants.SOCIAL_STATE] = FreeSMS.SocialState.FB_LOGOUT;
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            });
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            HideProgressIndicator();

            #region TWITTER
            if (socialNetwork == HikeConstants.ServerJsonKeys.TWITTER)
            {
            }
            #endregion
            #region FACEBOOK
            else if (socialNetwork == HikeConstants.ServerJsonKeys.FACEBOOK) // facebook auth
            {
                FacebookOAuthResult oauthResult;

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
                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack(); // take you to the proper page
                }
            }
            #endregion
            #region HIKE INVITE URL
            else
            {
            }
            #endregion
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
                            if (NavigationService.CanGoBack)
                                NavigationService.GoBack();
                        });
                    return;
                }

                var result = (IDictionary<string, object>)e.GetResultData();
                string id = (string)result["id"];
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.FB_ACCESS_TOKEN, accessToken);
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.FB_USER_ID, id);
                if (fromEnterName)
                {
                    string profilePictureUrl = string.Format("https://graph.facebook.com/{0}/picture?height={1}&width={1}", id, HikeConstants.PROFILE_PICS_SIZE);
                    WebClient client = new WebClient();
                    client.OpenReadAsync(new Uri(profilePictureUrl));
                    client.OpenReadCompleted += (ss, ee) =>
                    {
                        Stream s = ee.Result;
                        byte[] imgBytes = null;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (s != null)
                            {
                                try
                                {
                                    BitmapImage b = new BitmapImage();
                                    b.SetSource(s);
                                    imgBytes = UI_Utils.Instance.ConvertToBytes(b);
                                    MiscDBUtil.saveLargeImage(HikeConstants.MY_PROFILE_PIC, imgBytes);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("SocialPages.xaml :: LoginSucceded, LoginSucceded, Exception : " + ex.StackTrace);
                                }
                            }

                            if (imgBytes != null)
                                PhoneApplicationService.Current.State["img"] = imgBytes;
                            PhoneApplicationService.Current.State["fbName"] = (string)result["name"];
                            if (NavigationService.CanGoBack)
                                NavigationService.GoBack();
                        });
                    };
                }
                else
                {
                    HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.FB_LOGGED_IN, true);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PhoneApplicationService.Current.State[HikeConstants.SOCIAL_STATE] = FreeSMS.SocialState.FB_LOGIN;
                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                    });
                }

                // if this is called from Entername screen dont store logged in status

            };
            fb.GetAsync("me");
        }

        private void AuthenticateTwitter() // used for twitter
        {
            var requestTokenQuery = OAuthUtil.GetRequestTokenQuery();
            requestTokenQuery.RequestAsync(Social.TwitterSettings.RequestTokenUri, null);
            requestTokenQuery.QueryResponse += new EventHandler<WebQueryResponseEventArgs>(requestTokenQuery_QueryResponse);
        }

        private void requestTokenQuery_QueryResponse(object sender, WebQueryResponseEventArgs e)
        {
            try
            {
                StreamReader reader = new StreamReader(e.Response);
                string strResponse = reader.ReadToEnd();
                var parameters = GetQueryParameters(strResponse);
                oAuthTokenKey = parameters["oauth_token"];
                tokenSecret = parameters["oauth_token_secret"];
                var authorizeUrl = Social.TwitterSettings.AuthorizeUri + "?oauth_token=" + oAuthTokenKey;

                Dispatcher.BeginInvoke(() =>
                {
                    this.BrowserControl.Navigate(new Uri(authorizeUrl, UriKind.RelativeOrAbsolute));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SocialPages :: requestTokenQuery_QueryResponse : TwitterAuth , Exception:" + ex.StackTrace);
            }
        }

        public static Dictionary<string, string> GetQueryParameters(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                string splitItems = response;
                if (response.Contains('?'))
                {
                    string[] items1 = response.Split('?');
                    if (splitItems.Length > 1)
                        splitItems = items1[1];
                }
                Dictionary<string, string> nameValueCollection = new Dictionary<string, string>();
                string[] items = splitItems.Split('&');

                foreach (string item in items)
                {
                    if (item.Contains("="))
                    {
                        string[] nameValue = item.Split('=');
                        if (nameValue[0].Contains("?"))
                            nameValue[0] = nameValue[0].Replace("?", String.Empty);
                        nameValueCollection.Add(nameValue[0], System.Net.HttpUtility.UrlDecode(nameValue[1]));
                    }
                }
                return nameValueCollection;
            }
            return null;
        }

        private void Browser_Navigating(object sender, NavigatingEventArgs e)
        {
            ShowProgressIndicator();

            string uri = e.Uri.AbsoluteUri.ToString();
            if (socialNetwork == HikeConstants.ServerJsonKeys.TWITTER && uri.Contains(Social.TwitterSettings.OAuthTokenKey) && uri.Contains(Social.TwitterSettings.OAuthVerifierKey))
            {
                try
                {
                    Dictionary<string, string> authorizeResult = GetQueryParameters(e.Uri.ToString());
                    string verifyPin = authorizeResult["oauth_verifier"];
                    string oAuthTokenKey = authorizeResult["oauth_token"];
                    OAuthWebQuery accessTokenQuery = OAuthUtil.GetAccessTokenQuery(oAuthTokenKey, tokenSecret, verifyPin);

                    accessTokenQuery.QueryResponse += new EventHandler<WebQueryResponseEventArgs>(AccessTokenQuery_QueryResponse);
                    accessTokenQuery.RequestAsync(Social.TwitterSettings.AccessTokenUri, null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SocialPages::Browser_Navigating:TwitterAuth , Exception:" + ex.StackTrace);
                }
            }
            else if (uri.Contains("get.hike.in") && uri.Contains("windowsphone") || uri.Contains("zune"))
            {
                e.Cancel = true;
                Dispatcher.BeginInvoke(() =>
                    {
                        if (uri.Contains("denied") && NavigationService.CanGoBack)
                        {
                            if (NavigationService.CanGoBack)
                                NavigationService.GoBack();
                        }
                    });
            }
            else if (uri.Contains("invite") && (uri.Contains("Hike.in") || uri.Contains("hike.in")))
            {
                e.Cancel = true;
                Dispatcher.BeginInvoke(() =>
                {
                    Uri nextPage = new Uri("/View/InviteUsers.xaml", UriKind.Relative);
                    try
                    {
                        NavigationService.Navigate(nextPage);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SocialPages SCREEN :: Exception while navigating to Invite screen : " + ex.StackTrace);
                    }
                });
            }
        }

        void AccessTokenQuery_QueryResponse(object sender, WebQueryResponseEventArgs e)
        {
            try
            {
                StreamReader reader = new StreamReader(e.Response);
                string strResponse = reader.ReadToEnd();
                var parameters = GetQueryParameters(strResponse);
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.TWITTER_TOKEN, parameters["oauth_token"]);
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.TWITTER_TOKEN_SECRET, parameters["oauth_token_secret"]);
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.TW_LOGGED_IN, true);
                Dispatcher.BeginInvoke(() =>
                {
                    PhoneApplicationService.Current.State["socialState"] = FreeSMS.SocialState.TW_LOGIN;
                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SocialPages::AccessTokenQuery_QueryResponse, Exception:" + ex.StackTrace);
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnBackKeyPress(e);
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void BrowserControl_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            HideProgressIndicator();
        }

        private void ShowProgressIndicator()
        {
            loadingBar.IsIndeterminate = true;
        }

        private void HideProgressIndicator()
        {
            loadingBar.IsIndeterminate = false;
        }
    }
}