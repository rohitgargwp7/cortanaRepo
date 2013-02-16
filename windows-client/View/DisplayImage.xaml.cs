using System;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using windows_client.utils;
namespace windows_client.View
{
    public partial class DisplayImage : PhoneApplicationPage
    {
        private string msisdn;
        private string fileName;//name of file recived from server. it would be either msisdn or default avatr file name

        public DisplayImage()
        {
            InitializeComponent();
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("objectForFileTransfer");
            PhoneApplicationService.Current.State.Remove("displayProfilePic");
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("objectForFileTransfer"))
            {
                object[] fileTapped = (object[])PhoneApplicationService.Current.State["objectForFileTransfer"];
                long messsageId = (long)fileTapped[0];
                msisdn = (string)fileTapped[1];
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messsageId);
                byte[] filebytes;
                MiscDBUtil.readFileFromIsolatedStorage(filePath, out filebytes);
                setImage(filebytes);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey("displayProfilePic"))
            {
                object[] profilePicTapped = (object[])PhoneApplicationService.Current.State["displayProfilePic"];
                msisdn = (string)profilePicTapped[0];
                string filePath = msisdn + HikeConstants.FULL_VIEW_IMAGE_PREFIX;

                //check if image is already stored
                byte[] fullViewBytes = MiscDBUtil.getThumbNailForMsisdn(filePath);
                if (fullViewBytes != null && fullViewBytes.Length > 0)
                {
                    setImage(fullViewBytes);
                }
                else if (MiscDBUtil.hasCustomProfileImage(msisdn))
                {
                    fileName = msisdn + HikeConstants.FULL_VIEW_IMAGE_PREFIX;
                    loadingProgress.Opacity = 1;
                    if (!Utils.isGroupConversation(msisdn))
                    {
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/account/avatar/" + msisdn + "?fullsize=true", getProfilePic_Callback, true);
                    }
                    else
                    {
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/group/" + msisdn + "/avatar?fullsize=true", getProfilePic_Callback, true);
                    }
                }
                else
                {
                    fileName = UI_Utils.Instance.getDefaultAvatarFileName(msisdn,
                        Utils.isGroupConversation(msisdn));
                    byte[] defaultImageBytes = MiscDBUtil.getThumbNailForMsisdn(fileName);
                    if (defaultImageBytes == null || defaultImageBytes.Length == 0)
                    {
                        loadingProgress.Opacity = 1;
                        AccountUtils.createGetRequest(AccountUtils.AVATAR_BASE + "/static/avatars/" + fileName, getProfilePic_Callback, false);
                    }
                    else
                    {
                        setImage(defaultImageBytes);
                    }
                }
            }
        }

        public void getProfilePic_Callback(byte[] fullBytes)
        {
            if (fullBytes != null && fullBytes.Length > 0)
                MiscDBUtil.saveAvatarImage(fileName, fullBytes, false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                loadingProgress.Opacity = 0;
                if (fullBytes != null && fullBytes.Length > 0)
                {
                    setImage(fullBytes);
                }
                else
                {
                    byte[] smallThumbnailImage = MiscDBUtil.getThumbNailForMsisdn(msisdn);
                    if (smallThumbnailImage != null && smallThumbnailImage.Length > 0)
                    {
                        setImage(smallThumbnailImage);
                    }
                    else
                    {
                        BitmapImage defaultImage = null;
                        if (Utils.isGroupConversation(msisdn))
                        {
                            defaultImage = UI_Utils.Instance.getDefaultGroupAvatar(msisdn);
                        }
                        else
                        {
                            defaultImage = UI_Utils.Instance.getDefaultAvatar(msisdn);
                        }
                        this.FileImage.Source = defaultImage;
                    }
                }
            });
        }

        private void setImage(byte[] imageBytes)
        {
            MemoryStream memStream = new MemoryStream(imageBytes);
            memStream.Seek(0, SeekOrigin.Begin);
            BitmapImage fileImage = new BitmapImage();
            fileImage.SetSource(memStream);
            this.FileImage.Source = fileImage;
        }

        //private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        //{
        //    initialAngle = transform.Rotation;
        //    initialScale = transform.ScaleX;
        //}

        //private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        //{
        //    //transform.Rotation = initialAngle + e.TotalAngleDelta;
        //    transform.ScaleX = initialScale * e.DistanceRatio;
        //    transform.ScaleY = initialScale * e.DistanceRatio;
        //}


        //private void GestureListener_DragDelta(object sender, DragDeltaGestureEventArgs e)
        //{
        //    // if is not touch enabled or the scale is different than 1 then don’t allow moving
        //    if (transform.ScaleX <= 1.1)
        //        return;
        //    double centerX = transform.CenterX;
        //    double centerY = transform.CenterY;
        //    double translateX = transform.TranslateX;
        //    double translateY = transform.TranslateY;
        //    double scale = transform.ScaleX;
        //    double width = FileImage.ActualWidth;
        //    double height = FileImage.ActualHeight;

        //    // verify limits to not allow the image to get out of area

        //    if (centerX - scale * centerX + translateX + e.HorizontalChange < 0 &&
        //    centerX + scale * (width - centerX) + translateX + e.HorizontalChange > width)
        //    {
        //        transform.TranslateX += e.HorizontalChange;
        //    }

        //    if (centerY - scale * centerY + translateY + e.VerticalChange < 0 &&
        //    centerY + scale * (height - centerY) + translateY + e.VerticalChange > height)
        //    {
        //        transform.TranslateY += e.VerticalChange;
        //    }
        //    return;
        //}
    }
}