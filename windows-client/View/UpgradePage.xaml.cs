using Microsoft.Phone.Controls;
using Microsoft.Phone.UserData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.View
{
    public partial class UpgradePage : PhoneApplicationPage
    {
        private static List<ContactInfo> listContactInfo;
        public UpgradePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.UPGRADE_SCREEN);
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            string navigateTo = "/View/ConversationsList.xaml";//default page
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (a, b) =>
            {
                navigateTo = "/View/ConversationsList.xaml";
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                Thread.Sleep(2000);//added so that this shows at least for 2 sec
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, 1);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (a, b) =>
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                NavigationService.Navigate(new Uri(navigateTo, UriKind.Relative));
            };
        }
    }
}