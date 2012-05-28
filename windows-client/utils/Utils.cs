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

namespace windows_client.utils
{
    public class Utils
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public static void savedAccountCredentials(windows_client.utils.AccountUtils.AccountInfo accountInfo)
        {
            appSettings[HikeMessengerApp.MSISDN_SETTING] = accountInfo.msisdn;
            appSettings[HikeMessengerApp.UID_SETTING] = accountInfo.uid;
            appSettings[HikeMessengerApp.TOKEN_SETTING] = accountInfo.token;
            appSettings[HikeMessengerApp.SMS_SETTING] = accountInfo.smsCredits;
            appSettings.Save();
        }
    }
}
