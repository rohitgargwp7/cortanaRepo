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
    public partial class Walkthrough : PhoneApplicationPage
    {
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;

        public Walkthrough()
        {
            InitializeComponent();

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = "Done";
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            walkThrough.ApplicationBar = appBar;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
        }

        private void walkThroughPvt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (walkThroughPvt.SelectedIndex)
            {
                case 0:
                case 1:
                    nextIconButton.IsEnabled = false;
                    break;
                case 2:
                    nextIconButton.IsEnabled = true;
                    break;
            }
        }
    }
}