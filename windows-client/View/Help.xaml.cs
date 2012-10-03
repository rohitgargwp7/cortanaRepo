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
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class Help : PhoneApplicationPage
    {
        public Help()
        {
            InitializeComponent();
            if (utils.Utils.isDarkTheme())
            {
                this.made_with_love.Source = new BitmapImage(new Uri("images/made_with_love_dark.png", UriKind.Relative));
            }
            else
            {
                this.made_with_love.Source = new BitmapImage(new Uri("images/made_with_love.png", UriKind.Relative));
            }
        }

        private void FAQs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/FAQTerms.xaml?page=faq", UriKind.Relative));
        }

        private void ContactUs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbt = new WebBrowserTask();
            wbt.Uri = new Uri(Uri.EscapeUriString(HikeConstants.CONTACT_US_LINK), UriKind.RelativeOrAbsolute);
            wbt.Show();
        }

        private void Legal_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/FAQTerms.xaml?page=legal", UriKind.Relative));
        }

        private void Updates_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
        
        private void Walkthrough_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State["FromHelpPage"] = true;
            NavigationService.Navigate(new Uri("/View/Walkthrough.xaml", UriKind.Relative));
        }
    }
}