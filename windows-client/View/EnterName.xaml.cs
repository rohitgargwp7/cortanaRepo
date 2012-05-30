using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public EnterName()
        {
            InitializeComponent();
        }

        private void btnEnterName_Click(object sender, RoutedEventArgs e)
        {
            string name = txtBxEnterName.Text;
            AccountUtils.setName(name,new AccountUtils.postResponseFunction(setName_Callback));
        }

        private void setName_Callback(JObject obj)
        {
            Uri nextPage = null;

            if (obj == null || "ok" != (string)obj["stat"])
            {
                logger.Info("HTTP", "Unable to set name");
                // SHOW SOME TRY AGAIN MSG etc
                return;
            }
            appSettings[HikeMessengerApp.NAME_SETTING] = "y";
            appSettings.Save();
            nextPage = new Uri("/View/MessageList.xaml", UriKind.Relative);

            int count = 1;
            while (!App.Ab_scanned && count <=20)
            {
                Thread.Sleep(1 * 1000); //sleep for one second
                count++;
            }
            if (!App.Ab_scanned) // timeout occured
            {
                // SHOW NETWORK ERROR
            }
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(nextPage); });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
    }
}