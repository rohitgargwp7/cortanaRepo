using System;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;
using windows_client.Languages;
using windows_client.Model;
using System.Collections.Generic;

namespace windows_client.View
{
    public partial class Walkthrough : PhoneApplicationPage
    {
        bool isClicked = false;
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;
        private bool secondScreenSeen = false;

        public Walkthrough()
        {
            InitializeComponent();

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = AppResources.Walkthrough_LetsHike_Txt;
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            walkThrough.ApplicationBar = appBar;
        }
       
        private void doneBtn_Click(object sender, EventArgs e)
        {
            // this is done to avoid navigation exception
            if (isClicked)
                return;
            isClicked = true;

            NavigationService.GoBack();
        }

        private void walkThroughPvt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (walkThroughPvt.SelectedIndex)
            {
                case 0:
                    box0.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                    box1.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box2.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    nextIconButton.IsEnabled = false;
                    break;

                case 1:
                    secondScreenSeen = true;
                    box0.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box1.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                    box2.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    nextIconButton.IsEnabled = false;
                    break;
                case 2:
                    box0.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box1.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box2.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                    if (secondScreenSeen)
                        nextIconButton.IsEnabled = true;
                    break;
            }
        }
    }
}