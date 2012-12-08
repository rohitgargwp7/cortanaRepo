using System;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;
using windows_client.Languages;

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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SmileyParser.Instance.initializeSmileyParser();
            if (PhoneApplicationService.Current.State.ContainsKey("FromNameScreen")) // represents page is launched from entername screen
            {
                if(NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
        }
        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("FromNameScreen");
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            // this is done to avoid navigation exception
            if (isClicked)
                return;
            isClicked = true;
            if (PhoneApplicationService.Current.State.ContainsKey("FromNameScreen") || App.PageState.WALKTHROUGH_SCREEN == (App.PageState)App.appSettings[App.PAGE_STATE])
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                App.WriteToIsoStorageSettings(HikeConstants.IS_NEW_INSTALLATION, true);
                NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
            }
            else // this shows this page is from help page
            {
                NavigationService.GoBack();
            }
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
                    if(secondScreenSeen)
                        nextIconButton.IsEnabled = true;
                    break;
            }
        }
    }
}