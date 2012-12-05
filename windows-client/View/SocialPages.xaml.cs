﻿using System;
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

namespace windows_client.View
{
    public partial class SocialPages : PhoneApplicationPage
    {
        private bool isTwitter;
        private string token;
        private string tokenSecret;
        private string pin;

        private const string extendedPermissions = "user_about_me,publish_stream";
        private readonly FacebookClient _fb = new FacebookClient();

        public SocialPages()
        {
            InitializeComponent();
            PhoneApplicationService.Current.State["FromSocialPage"] = true;
            isTwitter = (bool)PhoneApplicationService.Current.State[HikeConstants.SOCIAL];
            if (isTwitter)
            {
                BrowserControl.IsScriptEnabled = false;
                AuthenticateTwitter();
            }
            else
            {
                if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN))
                    LogoutFb();
                else
                    LogInFb();
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL);
        }

        private void LogInFb()
        {
            var parameters = new Dictionary<string, object>();
            parameters["client_id"] = Misc.Social.FacebookSettings.AppID;
            parameters["redirect_uri"] = "https://www.facebook.com/connect/login_success.html";
            parameters["response_type"] = "token";
            parameters["display"] = "touch";
            parameters["scope"] = extendedPermissions;
            BrowserControl.Navigate(_fb.GetLoginUrl(parameters));
        }

        private void LogoutFb()
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
            #region TWITTER
            if (isTwitter)
            {
                try
                {
                    if (e.Uri.AbsoluteUri.ToLower().Replace("https://", "http://") == Misc.Social.TwitterSettings.AuthorizeUrl)
                    {
                        RetrieveAccessToken();
                        //var htmlString = BrowserControl.SaveToString();
                        //var pinFinder = new Regex(@"<DIV id=oauth_pin>(?<pin>[A-Za-z0-9_]+)</DIV>", RegexOptions.IgnoreCase);
                        //var match = pinFinder.Match(htmlString);
                        //if (match.Length > 0)
                        //{
                        //    var group = match.Groups["pin"];
                        //    if (group.Length > 0)
                        //    {
                        //        pin = group.Captures[0].Value;
                        //        if (!string.IsNullOrEmpty(pin))
                        //        {
                        //            RetrieveAccessToken();
                        //        }
                        //    }
                        //}
                        //if (string.IsNullOrEmpty(pin))
                        //{
                        //    Dispatcher.BeginInvoke(() =>
                        //        {
                        //            MessageBox.Show("Authorization denied by user");
                        //            NavigationService.GoBack();
                        //        });
                        //}
                        //// Make sure pin is reset to null
                        //pin = null;
                        //BrowserControl.Visibility = Visibility.Collapsed;
                    }
                }
                catch { }
            }
            #endregion
            #region FACEBOOK
            else // facebook auth
            {
                FacebookOAuthResult oauthResult;
                if (e.Uri.AbsoluteUri == "https://www.facebook.com/connect/login_success.html")
                {
                    App.RemoveKeyFromAppSettings("FbAccessToken");
                    App.RemoveKeyFromAppSettings("FbUserId");
                    App.RemoveKeyFromAppSettings(HikeConstants.FB_LOGGED_IN);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PhoneApplicationService.Current.State["socialState"] = FreeSMS.SocialState.FB_LOGOUT;
                        NavigationService.GoBack();
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
                    PhoneApplicationService.Current.State["socialState"] = FreeSMS.SocialState.FB_LOGIN;
                    NavigationService.GoBack();
                });
            };
            fb.GetAsync("me?fields=id");
        }

        private void AuthenticateTwitter() // used for twitter
        {
            // Create the Request
            var request = CreateRequest("POST", Misc.Social.TwitterSettings.RequestUrl);
            request.BeginGetResponse(result =>
            {
                try
                {
                    var req = result.AsyncState as HttpWebRequest;
                    if (req == null) throw new ArgumentNullException("result", "Request parameter is null");
                    using (var resp = req.EndGetResponse(result))
                    using (var strm = resp.GetResponseStream())
                    using (var reader = new StreamReader(strm))
                    {
                        var responseText = reader.ReadToEnd();
                        // Parse out the request token
                        ExtractTokenInfo(responseText);
                        // Navigate to the authorization Url
                        var loginUrl = new Uri(Misc.Social.TwitterSettings.AuthorizeUrl + "?" + Misc.Social.TwitterSettings.OAuthTokenKey + "=" + token);
                        Dispatcher.BeginInvoke(() => BrowserControl.Navigate(loginUrl));
                    }
                }
                catch
                {
                    Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show("Unable to retrieve request token");
                            NavigationService.GoBack();
                        });
                }
            }, request);
        }

        #region TWITTER HELPER FUNCTIONS
        private WebRequest CreateRequest(string httpMethod, string requestUrl)
        {
            var requestParameters = new Dictionary<string, string>();
            var secret = "";
            if (!string.IsNullOrEmpty(token))
            {
                requestParameters[Misc.Social.TwitterSettings.OAuthTokenKey] = token;
                secret = tokenSecret;
            }
            if (!string.IsNullOrEmpty(pin))
            {
                requestParameters[Misc.Social.TwitterSettings.OAuthVerifierKey] = pin;
            }
            var url = new Uri(requestUrl);
            var normalizedUrl = requestUrl;
            if (!string.IsNullOrEmpty(url.Query))
            {
                normalizedUrl = requestUrl.Replace(url.Query, "");
            }
            var signature = GenerateSignature(httpMethod, normalizedUrl, url.Query, requestParameters, secret);
            requestParameters[Misc.Social.TwitterSettings.OAuthSignatureKey] = UrlEncode(signature);
            var request = WebRequest.CreateHttp(normalizedUrl);
            request.Method = httpMethod;
            request.Headers[HttpRequestHeader.Authorization] = GenerateAuthorizationHeader(requestParameters);
            return request;
        }

        public string GenerateSignature(string httpMethod, string normalizedUrl, string queryString, IDictionary<string, string> requestParameters, string secret = null)
        {
            requestParameters[Misc.Social.TwitterSettings.OAuthConsumerKeyKey] = Misc.Social.TwitterSettings.ConsumerKey;
            requestParameters[Misc.Social.TwitterSettings.OAuthVersionKey] = Misc.Social.TwitterSettings.OAuthVersion;
            requestParameters[Misc.Social.TwitterSettings.OAuthNonceKey] = GenerateNonce();
            requestParameters[Misc.Social.TwitterSettings.OAuthTimestampKey] = GenerateTimeStamp();
            requestParameters[Misc.Social.TwitterSettings.OAuthSignatureMethodKey] = Misc.Social.TwitterSettings.Hmacsha1SignatureType;
            string signatureBase = GenerateSignatureBase(httpMethod, normalizedUrl, queryString, requestParameters);
            var hmacsha1 = new HMACSHA1();
            var key = string.Format("{0}&{1}", UrlEncode(Misc.Social.TwitterSettings.ConsumerSecret),
                                    string.IsNullOrEmpty(secret) ? "" : UrlEncode(secret));
            hmacsha1.Key = Encoding.UTF8.GetBytes(key);
            var signature = ComputeHash(signatureBase, hmacsha1);
            return signature;
        }

        private static readonly Random Random = new Random();

        public static string GenerateNonce()
        {
            // Random number between 123456 and 9999999
            return Random.Next(123456, 9999999).ToString();
        }

        public static string GenerateTimeStamp()
        {
            var now = DateTime.UtcNow;
            TimeSpan ts = now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        public static string GenerateSignatureBase(string httpMethod, string normalizedUrl, string queryString, IDictionary<string, string> requestParameters)
        {
            var parameters = new List<KeyValuePair<string, string>>(GetQueryParameters(queryString)) {
        new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthVersionKey, requestParameters[Misc.Social.TwitterSettings.OAuthVersionKey]),
        new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthNonceKey, requestParameters[Misc.Social.TwitterSettings.OAuthNonceKey]),
        new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthTimestampKey,
                                                                     requestParameters[Misc.Social.TwitterSettings.OAuthTimestampKey]),
        new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthSignatureMethodKey,
                                                                     requestParameters[Misc.Social.TwitterSettings.OAuthSignatureMethodKey]),
        new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthConsumerKeyKey,
                                                                     requestParameters[Misc.Social.TwitterSettings.OAuthConsumerKeyKey]) };
            if (requestParameters.ContainsKey(Misc.Social.TwitterSettings.OAuthVerifierKey))
            {
                parameters.Add(new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthVerifierKey,
                                               requestParameters[Misc.Social.TwitterSettings.OAuthVerifierKey]));
            }
            if (requestParameters.ContainsKey(Misc.Social.TwitterSettings.OAuthTokenKey))
            {
                parameters.Add(new KeyValuePair<string, string>(Misc.Social.TwitterSettings.OAuthTokenKey,
                                               requestParameters[Misc.Social.TwitterSettings.OAuthTokenKey]));
            }
            parameters.Sort((kvp1, kvp2) =>
            {
                if (kvp1.Key == kvp2.Key)
                {
                    return string.Compare(kvp1.Value, kvp2.Value);
                }
                return string.Compare(kvp1.Key, kvp2.Key);
            });
            var parameterString = BuildParameterString(parameters);
            if (requestParameters.ContainsKey(Misc.Social.TwitterSettings.OAuthPostBodyKey))
            {
                parameterString += "&" + requestParameters[Misc.Social.TwitterSettings.OAuthPostBodyKey];
            }
            var signatureBase = new StringBuilder();
            signatureBase.AppendFormat("{0}&", httpMethod);
            signatureBase.AppendFormat("{0}&", UrlEncode(normalizedUrl));
            signatureBase.AppendFormat("{0}", UrlEncode(parameterString));
            return signatureBase.ToString();
        }

        private static IEnumerable<KeyValuePair<string, string>> GetQueryParameters(string queryString)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(queryString)) return parameters;
            queryString = queryString.Trim('?');
            return (from pair in queryString.Split('&')
                    let bits = pair.Split('=')
                    where bits.Length == 2
                    select new KeyValuePair<string, string>(bits[0], bits[1])).ToArray();
        }

        private static string BuildParameterString(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var sb = new StringBuilder();
            foreach (var parameter in parameters)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.AppendFormat("{0}={1}", parameter.Key, parameter.Value);
            }
            return sb.ToString();
        }
        /// <summary>
        /// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
        /// </summary>
        private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };

        private static readonly char[] HexUpperChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string UrlEncode(string value)
        {
            // Start with RFC 2396 escaping by calling the .NET method to do the work.
            // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
            // If it does, the escaping we do that follows it will be a no-op since the
            // characters we search for to replace can't possibly exist in the string.
            var escaped = new StringBuilder(Uri.EscapeDataString(value));
            foreach (string t in UriRfc3986CharsToEscape)
            {
                escaped.Replace(t, HexEscape(t[0]));
            }
            return escaped.ToString();
        }

        public static string HexEscape(char character)
        {
            var to = new char[3];
            int pos = 0;
            EscapeAsciiChar(character, to, ref pos);
            return new string(to);
        }

        private static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = HexUpperChars[(ch & 240) >> 4];
            to[pos++] = HexUpperChars[ch & '\x000f'];
        }

        private static string ComputeHash(string data, HashAlgorithm hashAlgorithm)
        {
            byte[] dataBuffer = Encoding.UTF8.GetBytes(data);
            byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);
            return Convert.ToBase64String(hashBytes);
        }

        public static string GenerateAuthorizationHeader(IDictionary<string, string> requestParameters)
        {
            var paras = new StringBuilder();
            foreach (var param in requestParameters)
            {
                if (paras.Length > 0) paras.Append(",");
                paras.Append(param.Key + "=\"" + param.Value + "\"");
            }
            return "OAuth " + paras;
        }

        private IEnumerable<KeyValuePair<string, string>> ExtractTokenInfo(string responseText)
        {
            if (string.IsNullOrEmpty(responseText)) return null;
            var responsePairs = (from pair in responseText.Split('&')
                                 let bits = pair.Split('=')
                                 where bits.Length == 2
                                 select new KeyValuePair<string, string>(bits[0], bits[1])).ToArray();
            token = responsePairs
                          .Where(kvp => kvp.Key == Misc.Social.TwitterSettings.OAuthTokenKey)
                          .Select(kvp => kvp.Value).FirstOrDefault();
            tokenSecret = responsePairs
                                      .Where(kvp => kvp.Key == Misc.Social.TwitterSettings.OAuthTokenSecretKey)
                                      .Select(kvp => kvp.Value).FirstOrDefault();
            return responsePairs;
        }

        #endregion

        public void RetrieveAccessToken()
        {
            var request = CreateRequest("POST", Misc.Social.TwitterSettings.AccessUrl);
            request.BeginGetResponse(result =>
            {
                try
                {
                    var req = result.AsyncState as HttpWebRequest;
                    if (req == null) throw new ArgumentNullException("result", "Request is null");
                    using (var resp = req.EndGetResponse(result))
                    using (var strm = resp.GetResponseStream())
                    using (var reader = new StreamReader(strm))
                    {
                        var responseText = reader.ReadToEnd();
                        var userInfo = ExtractTokenInfo(responseText);
                        App.WriteToIsoStorageSettings("TwToken", token);
                        App.WriteToIsoStorageSettings("TwTokenSecret", tokenSecret);
                        App.WriteToIsoStorageSettings(HikeConstants.TW_LOGGED_IN, true);
                        Dispatcher.BeginInvoke(() =>
                        {
                            PhoneApplicationService.Current.State["socialState"] = FreeSMS.SocialState.TW_LOGIN;
                            NavigationService.GoBack();
                        });
                    }
                }
                catch
                {
                    Dispatcher.BeginInvoke(() =>
                        {
                            
                            //MessageBox.Show("Unable to retrieve Access Token");
                            //NavigationService.GoBack();
                        });
                }
            }, request);
        }

        private void Browser_Navigating(object sender, NavigatingEventArgs e)
        {

            string uri = e.Uri.AbsoluteUri.ToString();
            if (uri.Contains("get.hike.in") || uri.Contains("windowsphone"))
            {
                e.Cancel = true;
                Dispatcher.BeginInvoke(() =>
                    {
                        if (uri.Contains("denied") && NavigationService.CanGoBack)
                            NavigationService.GoBack();
                    });
            }
        }

    }
}