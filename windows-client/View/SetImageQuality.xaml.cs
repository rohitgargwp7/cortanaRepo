using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using windows_client.Languages;
using windows_client.utils;

namespace windows_client.View
{
    public partial class SetImageQuality : PhoneApplicationPage
    {
        ApplicationBarIconButton picturesUpload;

        public SetImageQuality()
        {
            InitializeComponent();
            InitialiseAppBar();
        }

        private void InitialiseAppBar()
        {
            ApplicationBar = new ApplicationBar()
            {
                ForegroundColor = (Color)App.Current.Resources["AppBarBlackForegroundColor"],
                BackgroundColor = (Color)App.Current.Resources["AppBarBlackBackgroundColor"],
            };

            picturesUpload = new ApplicationBarIconButton();
            picturesUpload.IconUri = new Uri("/View/images/AppBar/icon_send.png", UriKind.RelativeOrAbsolute);
            picturesUpload.Text = AppResources.Send_Txt;
            picturesUpload.Click += OnPicturesUploadClick;
            ApplicationBar.Buttons.Add(picturesUpload);
        }

        /// <summary>
        /// Uploads the images with selected Image Quality
        /// </summary>
        private void OnPicturesUploadClick(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.IMAGE_QUALITY] = ImageQualityBox.SelectedIndex;

            //previous page is preview Images page and then viewalbums page and then new chat thread page
            //So need to remove 2 entries from navigation service so that upload takes us directly to the chat thread page
            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();

            //clear thumbnail cache as it is not required now
            App.ViewModel.ClearMFtImageCache();
        }
        
        #region Page Functions

        #endregion
    }
}