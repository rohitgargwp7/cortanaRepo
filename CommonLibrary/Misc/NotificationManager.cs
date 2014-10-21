using CommonLibrary.Constants;
using CommonLibrary.Model;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Misc
{
    enum ToastType
    {
        MESSAGE,
        STATUS,
        FRIENDREQUEST
    }

    static class NotificationManager
    {
        public static void ShowNotification(ToastType type, string header, string content, string msisdn, bool isHidden)
        {
            bool isPushEnabled = true;
            HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.IS_PUSH_ENABLED, out isPushEnabled);

            if (!isPushEnabled)
                return;

            if (type == ToastType.STATUS)
            {
                byte statusSettingsValue;
                HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.STATUS_UPDATE_SETTING, out statusSettingsValue);

                if (statusSettingsValue == 0)
                    return;
            }

            try
            {
                ShellToast toast = new ShellToast();
                string navigationURI = null;
                toast.Title = isHidden && header == null ? String.Empty : header;
                toast.Content = content;

                switch (type)
                {
                    case ToastType.MESSAGE:
                        navigationURI = isHidden 
                            ? "/View/ConversationsList.xaml?sth=1&msisdn=" + msisdn
                            : "/View/ConversationsList.xaml?msisdn=" + msisdn;
                        break;

                    default:
                        navigationURI = "/View/ConversationsList.xaml?isStatus=true";
                        break;
                }

                toast.NavigationUri = new Uri(navigationURI, UriKind.Relative);
                toast.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ToastNotificationGenerator:: ShowToast, Exception at: " + ex.StackTrace);
            }
        }
    }
}
