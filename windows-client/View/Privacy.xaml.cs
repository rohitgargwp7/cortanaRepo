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
using System.Windows.Media.Imaging;

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage
    {
        public Privacy()
        {
            InitializeComponent();
            if (Utils.isDarkTheme())
            {
                this.unlinkAccount.Source = new BitmapImage(new Uri("images/unlink_account_white.png", UriKind.Relative));
                this.deleteAccount.Source = new BitmapImage(new Uri("images/delete_account_white.png", UriKind.Relative));
            }
            else
            {
                this.unlinkAccount.Source = new BitmapImage(new Uri("images/unlink_account_black.png", UriKind.Relative));
                this.deleteAccount.Source = new BitmapImage(new Uri("images/delete_account_black.png", UriKind.Relative));
            }
        }

        private void Unlink_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private void Delete_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
    }
}