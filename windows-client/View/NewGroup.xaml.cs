using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Tasks;
using windows_client.Languages;
using System.Windows.Media;
using windows_client.utils;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO;
using windows_client.DbUtils;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class NewGroup : PhoneApplicationPage
    {
        private bool reloadImage = true;
        private string group_name;
        public ApplicationBar appBar;
        public ApplicationBarIconButton nextIconButton;
        public ApplicationBarIconButton cameraIconButton;
        BitmapImage profileImage = null;
        PhotoChooserTask photoChooserTask = null;
        string mContactNumber;

        public NewGroup()
        {
            string uid = (string)HikeInstantiation.AppSettings[HikeConstants.UID_SETTING];
            mContactNumber = uid + ":" + TimeUtils.getCurrentTimeStamp();

            InitializeComponent();

            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["AppBarBackground"]).Color,
            };

            cameraIconButton = new ApplicationBarIconButton();
            cameraIconButton.IconUri = new Uri("/View/images/AppBar/icon_camera.png", UriKind.Relative);
            cameraIconButton.Text = AppResources.ChangePic_AppBar_Txt;
            cameraIconButton.Click += cameraIconButton_Click;
            appBar.Buttons.Add(cameraIconButton);
            ApplicationBar = appBar;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
            nextIconButton.Click += Next_Click;
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += photoChooserTask_Completed;

            groupCreateTip.Text = String.Format(AppResources.GroupCreateTip, HikeConstants.MAX_GROUP_MEMBER_SIZE);
        }

        byte[] fullViewImageBytes = null;
        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            progressBar.Opacity = 1;

            if (e.TaskResult == TaskResult.OK)
            {
                if (profileImage == null)
                    profileImage = new BitmapImage();

                profileImage.SetSource(e.ChosenPhoto);

                try
                {
                    WriteableBitmap writeableBitmap = new WriteableBitmap(profileImage);
                    using (var msLargeImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msLargeImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                        fullViewImageBytes = msLargeImage.ToArray();
                        MiscDBUtil.saveLargeImage(mContactNumber, fullViewImageBytes);
                    }

                    reloadImage = false;

                    avatarImage.Source = UI_Utils.Instance.createImageFromBytes(fullViewImageBytes);
                    PhoneApplicationService.Current.State[HikeConstants.HAS_CUSTOM_IMAGE] = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EnterName :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }

            progressBar.Opacity = 0;
            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) ? true : false;
            txtBxEnterName.IsEnabled = true;
        }

        bool isClicked;

        private void Next_Click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            isClicked = true;

            Focus();

            PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP] = true;
            PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME] = group_name;
            PhoneApplicationService.Current.State[HikeConstants.NEW_GROUP_ID] = mContactNumber;

            var nextPage = new Uri("/View/ForwardTo.xaml", UriKind.Relative);
            isClicked = false;

            NavigationService.Navigate(nextPage);
            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;
        }

        private void cameraIconButton_Click(object sender, EventArgs e)
        {
            ChangeProfile();
        }

        private void ChangeProfile_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ChangeProfile();
        }

        private void ChangeProfile()
        {
            try
            {
                photoChooserTask.Show();
                nextIconButton.IsEnabled = false;
                txtBxEnterName.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EnterName :: OnProfilePicButtonTap, Exception : " + ex.StackTrace);
            }
        }

        private void txtBxEnterName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            group_name = txtBxEnterName.Text.Trim();

            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(group_name) ? true : false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || HikeInstantiation.IsTombstoneLaunch)
            {
                object obj = null;

                if (HikeInstantiation.IsTombstoneLaunch) /* ****************************    HANDLING TOMBSTONE    *************************** */
                {
                    if (State.TryGetValue(HikeConstants.GROUP_NAME, out obj))
                    {
                        txtBxEnterName.Text = (string)obj;
                        txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
                    }

                    if (State.TryGetValue("txtBxEnterName", out obj))
                    {
                        txtBxEnterName.Text = (string)obj;
                        txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
                        obj = null;
                    }
                }
            }

            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) ? true : false;

            if (reloadImage) // this will handle both deactivation and tombstone
            {
                if (State.ContainsKey("img"))
                {
                    fullViewImageBytes = (byte[])State["img"];

                    MemoryStream memStream = new MemoryStream(fullViewImageBytes);
                    memStream.Seek(0, SeekOrigin.Begin);

                    if (profileImage == null)
                        profileImage = new BitmapImage();

                    profileImage.SetSource(memStream);

                    reloadImage = false;

                    State.Remove("img");
                }
                else
                {
                    fullViewImageBytes = MiscDBUtil.getLargeImageForMsisdn(mContactNumber);

                    if (fullViewImageBytes != null)
                    {
                        try
                        {
                            MemoryStream memStream = new MemoryStream(fullViewImageBytes);
                            memStream.Seek(0, SeekOrigin.Begin);
                            BitmapImage empImage = new BitmapImage();
                            empImage.SetSource(memStream);
                            avatarImage.Source = empImage;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Enter Name ::  OnNavigatedTo , Exception : " + ex.StackTrace);
                            avatarImage.Source = UI_Utils.Instance.getDefaultGroupAvatar(mContactNumber, true);
                        }
                    }
                    else
                    {
                        avatarImage.Source = UI_Utils.Instance.getDefaultGroupAvatar(mContactNumber, true);
                    }
                }
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            PhoneApplicationService.Current.State.Remove(HikeConstants.START_NEW_GROUP);
            PhoneApplicationService.Current.State.Remove(HikeConstants.NEW_GROUP_ID);
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_NAME);
            PhoneApplicationService.Current.State.Remove(HikeConstants.HAS_CUSTOM_IMAGE);
            MiscDBUtil.DeleteImageForMsisdn(mContactNumber);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("fromEnterName");
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            string uri = e.Uri.ToString();

            if (!uri.Contains("View"))
            {
                if (!string.IsNullOrWhiteSpace(txtBxEnterName.Text))
                    State["txtBxEnterName"] = txtBxEnterName.Text;
                else
                    State.Remove("txtBxEnterName");

                State.Remove("img");
                if (fullViewImageBytes != null)
                    State["img"] = fullViewImageBytes;
            }
            else
                HikeInstantiation.IsTombstoneLaunch = false;
        }
    }
}