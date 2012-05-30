using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;

namespace windows_client.utils
{
    public class Utils
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public static void savedAccountCredentials(JObject obj)
        {
            AccountUtils.Token = (string)obj["token"];
            appSettings[HikeMessengerApp.MSISDN_SETTING] = (string)obj["msisdn"];
            appSettings[HikeMessengerApp.UID_SETTING] = (string)obj["uid"];
            appSettings[HikeMessengerApp.TOKEN_SETTING] = (string)obj["token"];
            appSettings[HikeMessengerApp.SMS_SETTING] =  (int)obj[NetworkManager.SMS_CREDITS];
            appSettings.Save();
        }
    }
}
