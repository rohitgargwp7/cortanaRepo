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
using windows_client.utils;

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
            nextIconButton.Text = "let's hike";
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            walkThrough.ApplicationBar = appBar;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
        private void doneBtn_Click(object sender, EventArgs e)
        {
            // this is done to avoid navigation exception
            if (isClicked)
                return;
            isClicked = true;
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
            NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
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
                    if (swipeLeft.Visibility == Visibility.Collapsed)
                        swipeLeft.Visibility = Visibility.Visible;
                    break;
                    
                case 1:
                    secondScreenSeen = true;
                    box0.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box1.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                    box2.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    nextIconButton.IsEnabled = false;
                    if (swipeLeft.Visibility == Visibility.Collapsed)
                        swipeLeft.Visibility = Visibility.Visible;
                    break;
                case 2:
                    box0.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box1.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
                    box2.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                    if(secondScreenSeen)
                        nextIconButton.IsEnabled = true;
                    if (swipeLeft.Visibility == Visibility.Visible)
                        swipeLeft.Visibility = Visibility.Collapsed;
                    break;
            }
        }
    }
}