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
            applicationVersion.Text = utils.Utils.GetVersion();
        }

        private void FAQs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.FAQS_LINK, UriKind.Absolute);
            webBrowserTask.Show();
        }

        private void ContactUs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask contactUsMail = new EmailComposeTask();
            contactUsMail.To = "support@bsb.in";
            try
            {
                contactUsMail.Show();
            }
            catch
            {
            }
        }

        private void Legal_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.TERMS_LINK, UriKind.Absolute);
            webBrowserTask.Show();
        }

        private void Updates_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
        
        private void Walkthrough_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Walkthrough.xaml", UriKind.Relative));
        }

        private void rateAndReview_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }
    }
}