using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.Languages;
using windows_client.DbUtils;
using windows_client;

namespace windows_client.View
{
    public partial class TutorialScreen : PhoneApplicationPage
    {
        ApplicationBarIconButton nextIconButton;
        App.PageState currentPagestate;

        public TutorialScreen()
        {
            InitializeComponent();

            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;
            this.ApplicationBar = appBar;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
            nextIconButton.Click += OnNextClick;
            appBar.Buttons.Add(nextIconButton);


            App.appSettings.TryGetValue(App.PAGE_STATE, out currentPagestate);

            switch (currentPagestate)
            {
                case App.PageState.TUTORIAL_SCREEN_STATUS:
                    InitialiseTutStatusUpdateScreen();
                    break;
                default:
                    InitialiseTutStickersScreen();
                    break;
            }
        }

        private void InitialiseTutStatusUpdateScreen()
        {
            gridStatusUpdates.Visibility = Visibility.Visible;
            gridStickers.Visibility = Visibility.Collapsed;
        }

        private void InitialiseTutStickersScreen()
        {
            gridStatusUpdates.Visibility = Visibility.Collapsed;
            gridStickers.Visibility = Visibility.Visible;
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Done_Btn;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
        public void OnNextClick(object sender, EventArgs e)
        {
            if (currentPagestate == App.PageState.TUTORIAL_SCREEN_STATUS)
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.TUTORIAL_SCREEN_STICKERS);
                InitialiseTutStickersScreen();
                currentPagestate = App.PageState.TUTORIAL_SCREEN_STICKERS;
            }
            else
            {
                SmileyParser.Instance.initializeSmileyParser();
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
            }
        }
    }
}