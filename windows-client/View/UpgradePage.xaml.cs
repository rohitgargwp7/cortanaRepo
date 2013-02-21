using Microsoft.Phone.Controls;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;

namespace windows_client.View
{
    public partial class UpgradePage : PhoneApplicationPage
    {

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
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (a, b) =>
            {
                Thread.Sleep(4000);
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.UPGRADE_SCREEN);
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, 1);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (a, b) =>
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                NavigationService.Navigate(new Uri("/View/NUX_InviteFriends.xaml",UriKind.Relative));
            };
        }
    }
}