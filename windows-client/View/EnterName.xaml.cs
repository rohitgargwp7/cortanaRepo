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

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        public EnterName()
        {
            InitializeComponent();
        }

        private void onNameEntered(object sender, RoutedEventArgs e)
        {
            Uri nextPage = new Uri("/View/MessageList.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
            prefUtils.savePreference("CurrentPage", nextPage);
        }
    }
}