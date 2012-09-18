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
    public partial class EditProfile : PhoneApplicationPage
    {
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;

        public EditProfile()
        {
            InitializeComponent();
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = "done";
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = true;
            appBar.Buttons.Add(nextIconButton);
            editProfile.ApplicationBar = appBar;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
        }
    }
}