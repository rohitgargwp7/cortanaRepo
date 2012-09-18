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
using Microsoft.Phone.Shell;

namespace windows_client.View
{

    public partial class Help : PhoneApplicationPage
    {
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;
        public Help()
        {
            InitializeComponent();
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
//            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = "accept";
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = true;
            appBar.Buttons.Add(nextIconButton);
            help.ApplicationBar = appBar;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));

        }

    }
}