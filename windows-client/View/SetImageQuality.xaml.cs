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
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class SetImageQuality : PhoneApplicationPage
    {
        ApplicationBarIconButton picturesUpload;

        private CameraCaptureTask cameraCaptureTask;

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
            
            if (!PhoneApplicationService.Current.State.ContainsKey(HikeConstants.CAMERA_IMAGE))
            {
                //previous page is preview Images page and then viewalbums page and then new chat thread page
                //So need to remove 2 entries from navigation service so that upload takes us directly to the chat thread page
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();                
            }

            if (NavigationService.CanGoBack)
                NavigationService.GoBack();

            //clear thumbnail cache as it is not required now
            App.ViewModel.ClearMFtImageCache();
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {            
            if (e.TaskResult == TaskResult.OK)
            {
                BitmapImage image = new BitmapImage();
                image.SetSource(e.ChosenPhoto);
                try
                {
                    PhoneApplicationService.Current.State[HikeConstants.CAMERA_IMAGE] = UI_Utils.Instance.ConvertToBytes(image);
                   
                    if (!App.appSettings.Contains(HikeConstants.SET_IMAGE_QUALITY))
                    {
                        ContentGrid.Visibility = Visibility.Visible;
                        ApplicationBar.IsVisible = true;
                    }
                    else
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (NavigationService.CanGoBack)
                                NavigationService.GoBack();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SetImageQuality :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                    });
            }
        }
       
        #region Page Functions
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New || App.IS_TOMBSTONED)
            {
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SELECT_CAMERA_IMAGE))
                {
                    ContentGrid.Visibility = Visibility.Collapsed;
                    ApplicationBar.IsVisible = false;

                    cameraCaptureTask = new CameraCaptureTask();
                    cameraCaptureTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
                    cameraCaptureTask.Show();
                    PhoneApplicationService.Current.State.Remove(HikeConstants.SELECT_CAMERA_IMAGE);
                }
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SELECT_CAMERA_IMAGE) || PhoneApplicationService.Current.State.ContainsKey(HikeConstants.CAMERA_IMAGE))
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.CAMERA_IMAGE);
                PhoneApplicationService.Current.State.Remove(HikeConstants.SELECT_CAMERA_IMAGE);

                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }

            base.OnBackKeyPress(e);
        }
        #endregion
    }
}