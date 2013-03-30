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
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using windows_client.View;

namespace windows_client.View
{
    public partial class PostStatus : PhoneApplicationPage
    {
        private ApplicationBar appBar;
        private ApplicationBarIconButton postStatusIcon;
        private bool isFacebookPost;
        private bool isTwitterPost;
        private bool isFirstLoad = true;
        private int moodId = 0;
        string lastMoodText = string.Empty;
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (isFirstLoad)
            {
                moodListBox.ItemsSource = MoodsInitialiser.Instance.MoodList;
                userImage.Source = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
                isFirstLoad = false;
            }
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FROM_SOCIAL_PAGE)) // shows page is navigated from social page
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.FROM_SOCIAL_PAGE);

                object oo;
                FreeSMS.SocialState ss = FreeSMS.SocialState.DEFAULT;
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.SOCIAL_STATE, out oo))
                {
                    ss = (FreeSMS.SocialState)oo;
                    PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL_STATE);
                }
                switch (ss)
                {
                    case FreeSMS.SocialState.FB_LOGIN:
                        JObject oj = new JObject();
                        oj["id"] = (string)App.appSettings[HikeConstants.AppSettings.FB_USER_ID];
                        oj["token"] = (string)App.appSettings[HikeConstants.AppSettings.FB_ACCESS_TOKEN];
                        oj["post"] = false;//so that hike promotional post is not posted on fb
                        AccountUtils.SocialPost(oj, new AccountUtils.postResponseFunction(SocialPostFB), HikeConstants.FACEBOOK, true);
                        break;

                    case FreeSMS.SocialState.TW_LOGIN:
                        JObject ojj = new JObject();
                        ojj["id"] = (string)App.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN]; ;
                        ojj["token"] = (string)App.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN_SECRET];
                        ojj["post"] = false;
                        AccountUtils.SocialPost(ojj, new AccountUtils.postResponseFunction(SocialPostTW), HikeConstants.TWITTER, true);
                        break;
                }
            }
        }

        private void btnPostStatus_Click(object sender, EventArgs e)
        {

            string statusText = txtStatus.Text.Trim();
            if (statusText == string.Empty)
            {
                postStatusIcon.IsEnabled = true;
                return;
            }

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                postStatusIcon.IsEnabled = true;
                return;
            }
            JObject statusJSON = new JObject();
            statusJSON["status-message"] = statusText;
            if (isFacebookPost)
                statusJSON["fb"] = true;
            if (isTwitterPost)
                statusJSON["twitter"] = true;
            if (moodId > 0)
            {
                statusJSON["mood"] = moodId;
                statusJSON["timeofday"] = (int)TimeUtils.GetTimeIntervalDay();
            }
            AccountUtils.postStatus(statusJSON, StatusUpdateHelper.Instance.postStatus_Callback);
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();

        }

        void PostStatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtStatus.Focus();
            this.Loaded -= PostStatusPage_Loaded;
        }
        private void FbIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isFacebookPost)
            {
                if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN)) // already logged in
                {
                    fbIconImage.Source = UI_Utils.Instance.FacebookEnabledIcon;
                    isFacebookPost = true;
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.FACEBOOK;
                    NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                }
            }
            else
            {
                fbIconImage.Source = UI_Utils.Instance.FacebookDisabledIcon;
                isFacebookPost = false;
            }
        }

        private void TwitterIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isTwitterPost)
            {
                if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN)) // already logged in
                {
                    twitterIconImage.Source = UI_Utils.Instance.TwitterEnabledIcon;
                    isTwitterPost = true;
                    if (txtStatus.Text.Length > 140)
                        postStatusIcon.IsEnabled = false;
                    txtCounter.Visibility = Visibility.Visible;
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.TWITTER;
                    NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                }
            }
            else
            {
                twitterIconImage.Source = UI_Utils.Instance.TwitterDisabledIcon;
                isTwitterPost = false;
                if (txtStatus.Text.Length > 0)
                    postStatusIcon.IsEnabled = true;
                txtCounter.Visibility = Visibility.Collapsed;
            }
        }

        private void Mood_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            gridMood.Visibility = Visibility.Visible;
            this.appBar.IsVisible = false;
        }


        private void moodList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            windows_client.utils.MoodsInitialiser.Mood mood = this.moodListBox.SelectedItem as windows_client.utils.MoodsInitialiser.Mood;
            if (mood == null)
                return;
            moodId = moodListBox.SelectedIndex + 1;
            if (string.IsNullOrWhiteSpace(txtStatus.Text) || lastMoodText == txtStatus.Text)
                lastMoodText = txtStatus.Text = mood.MoodText;
            userImage.Source = mood.MoodImage;
            gridMood.Visibility = Visibility.Collapsed;
            moodIconImage.Source = UI_Utils.Instance.MoodEnabledIcon;
            this.appBar.IsVisible = true;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (gridMood.Visibility == Visibility.Visible)
            {
                gridMood.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                this.appBar.IsVisible = true;
                return;
            }
            base.OnBackKeyPress(e);
        }

        private void txtStatus_GotFocus(object sender, RoutedEventArgs e)
        {
            string name;
            App.appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            string nameToShow = null;
            if (!string.IsNullOrEmpty(name))
            {
                string[] nameArray = name.Split(' ');
                if (nameArray.Length > 0)
                {
                    nameToShow = nameArray[0];
                }
            }
            txtStatus.Hint = string.Format(AppResources.PostStatus_WhatsUp_Hint_txt, (nameToShow != null ? nameToShow : string.Empty));
        }

        public void SocialPostFB(JObject obj)
        {
            if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    fbIconImage.Source = UI_Utils.Instance.FacebookEnabledIcon;
                    isFacebookPost = true;
                });
            }
            else
            {
            }
        }

        public void SocialPostTW(JObject obj)
        {
            if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    twitterIconImage.Source = UI_Utils.Instance.TwitterEnabledIcon;
                    isTwitterPost = true;
                    if (txtStatus.Text.Length > 140)
                    {
                        postStatusIcon.IsEnabled = false;
                    }
                    txtCounter.Visibility = Visibility.Visible;
                });
            }
        }

        private void txtStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            int count = txtStatus.Text.Length;
            if (count == 0)
            {
                postStatusIcon.IsEnabled = false;
            }
            else if (isTwitterPost && count > 140)
            {
                postStatusIcon.IsEnabled = false;
            }
            else
            {
                postStatusIcon.IsEnabled = true;
            }
            txtCounter.Text = (140 - count).ToString();
        }
    }
}