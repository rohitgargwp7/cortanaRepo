using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class WelcomeScreen : PhoneApplicationPage
    {

        public WelcomeScreen()
        {
            InitializeComponent();

            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;
            this.ApplicationBar = appBar;

            ApplicationBarIconButton nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
            nextIconButton.Click += OnNextClick;
            appBar.Buttons.Add(nextIconButton);
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.WELCOME_HIKE_SCREEN);

            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);
            if (!string.IsNullOrEmpty(country_code) && country_code != "+91")
            {
                txtBlkInfo1.Text = AppResources.ReadyToHike_Txt;
                txtBlkInfo2.Visibility = Visibility.Collapsed;
            }

        }
        public void OnNextClick(object sender, EventArgs e)
        {
            List<ContactInfo> listContactInfo;
            SmileyParser.Instance.initializeSmileyParser();
            Uri nextPage;
            if (App.appSettings.TryGetValue(HikeConstants.CLOSE_FRIENDS_NUX, out listContactInfo) && listContactInfo.Count > 2)
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.NUX_SCREEN);
                nextPage = new Uri("/View/NUX_InviteFriends.xaml", UriKind.Relative);
            }
            else
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                nextPage = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
            }
            NavigationService.Navigate(nextPage);
        }
    }
}