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
            nextIconButton.Text = AppResources.AppBar_Done_Btn;
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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
        public void OnNextClick(object sender, EventArgs e)
        {
            SmileyParser.Instance.initializeSmileyParser();
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.NUX_SCREEN);
            NavigationService.Navigate(new Uri("/View/NUX_InviteFriends.xaml", UriKind.Relative));
        }
    }
}