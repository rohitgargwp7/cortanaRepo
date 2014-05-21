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
using Microsoft.Phone.Notification;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using System.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class MqttPreferences : PhoneApplicationPage
    {
        public MqttPreferences()
        {
            InitializeComponent();
            initializeBaseOnState();
        }

        private void initializeBaseOnState()
        {
            bool mqttDmqttToggle = true;
            App.appSettings.TryGetValue<bool>(App.MQTT_DMQTT_SETTING, out mqttDmqttToggle);
            this.mqttDmqttToggle.IsChecked = mqttDmqttToggle;
            if (mqttDmqttToggle)
                this.mqttDmqttToggle.Content = "MQTT";
            else
                this.mqttDmqttToggle.Content = "DMQTT";

            App.appSettings.TryGetValue<bool>(App.DNS_NODNS_SETTING, out mqttDmqttToggle);
            this.dnsNoDnsToggle.IsChecked = mqttDmqttToggle;
            if (mqttDmqttToggle)
                this.dnsNoDnsToggle.Content = "DNS";
            else
                this.dnsNoDnsToggle.Content = "IP";

        }

        private void mqttDmqttToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.mqttDmqttToggle.Content = "MQTT";
            App.WriteToIsoStorageSettings(App.MQTT_DMQTT_SETTING, true);
        }

        private void mqttDmqttToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.mqttDmqttToggle.Content = "DMQTT";
            App.WriteToIsoStorageSettings(App.MQTT_DMQTT_SETTING, false);
        }

        private void dnsIpToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.dnsNoDnsToggle.Content = "DNS";
            App.WriteToIsoStorageSettings(App.DNS_NODNS_SETTING, true);

        }

        private void dnsIPToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dnsNoDnsToggle.Content = "IP";
            App.WriteToIsoStorageSettings(App.DNS_NODNS_SETTING, false);
        }

        private void ClearLogs_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Logging.LogWriter.Instance.ClearLogs();
            MQttLogging.LogWriter.Instance.ClearLogs();
            MessageBox.Show("Cleared successfully.:)");
        }

        private void ViewLogs_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.VIEW_MORE_MESSAGE_OBJ] = Logging.LogWriter.Instance.ReadFile();
            var currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            currentPage.NavigationService.Navigate(new Uri("/View/ViewMessage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ViewMqtt_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.VIEW_MORE_MESSAGE_OBJ] = MQttLogging.LogWriter.Instance.ReadFile();
            var currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            currentPage.NavigationService.Navigate(new Uri("/View/ViewMessage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void Email_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string text = MQttLogging.LogWriter.Instance.ReadFile();
            if (text.Length > 32600)
            {
                MessageBox.Show("Log file greater than max email size, text would be trimmed");
                text = text.Substring(0, 32600);
            }

            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "Mqtt logs";
            emailComposeTask.Body = text;

            emailComposeTask.Show();
        }

        private void EmailApp_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string text = Logging.LogWriter.Instance.ReadFile();
            if (text.Length > 32767)
            {
                MessageBox.Show("Log file greater than max email size");
                text = text.Substring(0, 32767);
            }

            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "App logs";
            emailComposeTask.Body = text;

            emailComposeTask.Show();
        }


    }
}