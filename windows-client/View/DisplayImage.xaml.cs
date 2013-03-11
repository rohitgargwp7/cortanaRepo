using System;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using windows_client.utils;
using System.Diagnostics;
using System.Windows.Media;
using windows_client.Controls.StatusUpdate;
namespace windows_client.View
{
    public partial class DisplayImage : PhoneApplicationPage
    {
        private string msisdn;
        public DisplayImage()
        {
            InitializeComponent();
        }

        #region IMAGE ASSIGNMENT
        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("objectForFileTransfer");
            PhoneApplicationService.Current.State.Remove("displayProfilePic");
            PhoneApplicationService.Current.State.Remove(HikeConstants.IMAGE_TO_DISPLAY);
            PhoneApplicationService.Current.State.Remove(HikeConstants.STATUS_IMAGE_TO_DISPLAY);
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //TODO - use constants rather hard coded strings - MG
            if (PhoneApplicationService.Current.State.ContainsKey("objectForFileTransfer"))
            {
                object[] fileTapped = (object[])PhoneApplicationService.Current.State["objectForFileTransfer"];
                long messsageId = (long)fileTapped[0];
                msisdn = (string)fileTapped[1];
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messsageId);
                byte[] filebytes;
                MiscDBUtil.readFileFromIsolatedStorage(filePath, out filebytes);
                this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(filebytes);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey("displayProfilePic"))
            {
                string fileName;
                object[] profilePicTapped = (object[])PhoneApplicationService.Current.State["displayProfilePic"];
                msisdn = (string)profilePicTapped[0];
                string filePath = msisdn + HikeConstants.FULL_VIEW_IMAGE_PREFIX;
                //check if image is already stored
                byte[] fullViewBytes = MiscDBUtil.getThumbNailForMsisdn(filePath);
                if (fullViewBytes != null && fullViewBytes.Length > 0)
                {
                    this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(fullViewBytes);
                }
                else if (MiscDBUtil.hasCustomProfileImage(msisdn))
                {
                    fileName = msisdn + HikeConstants.FULL_VIEW_IMAGE_PREFIX;
                    loadingProgress.Opacity = 1;
                    if (!Utils.isGroupConversation(msisdn))
                    {
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/account/avatar/" + msisdn + "?fullsize=true", getProfilePic_Callback, true, fileName);
                    }
                    else
                    {
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/group/" + msisdn + "/avatar?fullsize=true", getProfilePic_Callback, true, fileName);
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
                        AccountUtils.createGetRequest(AccountUtils.AVATAR_BASE + "/static/avatars/" + fileName, getProfilePic_Callback, false, fileName);
                    }
                    else
                    {
                        this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(defaultImageBytes);
                    }
                }
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IMAGE_TO_DISPLAY))
            {
                BitmapImage imageToDisplay = (BitmapImage)PhoneApplicationService.Current.State[HikeConstants.IMAGE_TO_DISPLAY];
                this.FileImage.Source = imageToDisplay;
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.STATUS_IMAGE_TO_DISPLAY))
            {
                ImageStatusUpdate imageStatus = (ImageStatusUpdate)PhoneApplicationService.Current.State[HikeConstants.STATUS_IMAGE_TO_DISPLAY];
                byte[] statusImageBytes = null;
                bool isThumbnail;
                MiscDBUtil.getStatusUpdateImage(imageStatus.Msisdn, imageStatus.MappedStatusId, out statusImageBytes, out isThumbnail);
                this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(statusImageBytes);
            }
        }
        public void getProfilePic_Callback(byte[] fullBytes, object fName)
        {
            string fileName = fName as string;
            if (fullBytes != null && fullBytes.Length > 0)
                MiscDBUtil.saveAvatarImage(fileName, fullBytes, false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                loadingProgress.Opacity = 0;
                if (fullBytes != null && fullBytes.Length > 0)
                    this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(fullBytes);
                else
                    this.FileImage.Source = UI_Utils.Instance.GetBitmapImage(msisdn);
            });
        }
        #endregion

        #region PINCH AND ZOOM
        private bool isRestore = false;
        // these two fields fully define the zoom state:
        private double totalImageScale = 1d;
        private Point imagePosition = new Point(0, 0);
        private const double MAX_IMAGE_ZOOM = 15;
        private Point _oldFinger1;
        private Point _oldFinger2;
        private double _oldScaleFactor;

        #region Event handlers

        /// <summary>
        /// Initializes the zooming operation
        /// </summary>
        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            _oldFinger1 = e.GetPosition(FileImage, 0);
            _oldFinger2 = e.GetPosition(FileImage, 1);
            _oldScaleFactor = 1;
        }

        /// <summary>
        /// Computes the scaling and translation to correctly zoom around your fingers.
        /// </summary>
        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            var scaleFactor = e.DistanceRatio / _oldScaleFactor;
            if (totalImageScale * scaleFactor < 1)
            {
                isRestore = true;
            }
            else
            {
                isRestore = false;
                if (totalImageScale * scaleFactor > MAX_IMAGE_ZOOM)
                {
                    return;
                }
            }
            var currentFinger1 = e.GetPosition(FileImage, 0);
            var currentFinger2 = e.GetPosition(FileImage, 1);
            var translationDelta = GetTranslationDelta(currentFinger1, currentFinger2, _oldFinger1, _oldFinger2, imagePosition, scaleFactor);
            _oldFinger1 = currentFinger1;
            _oldFinger2 = currentFinger2;
            _oldScaleFactor = e.DistanceRatio;
            UpdateImageScale(scaleFactor);
            if (isRestore) //image's total reduction is < 1
            {
                imagePosition.X = (FileImage.ActualWidth * (1 - scaleFactor * totalImageScale)) / 2;
                imagePosition.Y = (FileImage.ActualHeight * (1 - scaleFactor * totalImageScale)) / 2;
                ApplyPosition();
            }
            else
            {
                UpdateImagePosition(translationDelta);
            }
        }

        /// <summary>
        /// Moves the image around following your finger.
        /// </summary>
        private void OnDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            var translationDelta = new Point(e.HorizontalChange, e.VerticalChange);

            if (IsDragValid(1, translationDelta))
                UpdateImagePosition(translationDelta);
        }

        /// <summary>
        /// Resets the image scaling and position
        /// </summary>
        private void OnDoubleTap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            ResetImagePosition();
        }

        private void OnPinchCompleted(object sender, PinchGestureEventArgs e)
        {
            if (isRestore)
            {
                ResetImagePosition();
            }
            isRestore = false;
        }

        #endregion

        #region Utils

        /// <summary>
        /// Computes the translation needed to keep the image centered between your fingers.
        /// </summary>
        private Point GetTranslationDelta(Point currentFinger1, Point currentFinger2,
            Point oldFinger1, Point oldFinger2, Point currentPosition, double scaleFactor)
        {
            var newPos1 = new Point(currentFinger1.X + (currentPosition.X - oldFinger1.X) * scaleFactor,
             currentFinger1.Y + (currentPosition.Y - oldFinger1.Y) * scaleFactor);
            var newPos2 = new Point(currentFinger2.X + (currentPosition.X - oldFinger2.X) * scaleFactor,
             currentFinger2.Y + (currentPosition.Y - oldFinger2.Y) * scaleFactor);
            var newPos = new Point((newPos1.X + newPos2.X) / 2, (newPos1.Y + newPos2.Y) / 2);
            return new Point(newPos.X - currentPosition.X, newPos.Y - currentPosition.Y);
        }

        /// <summary>
        /// Updates the scaling factor by multiplying the delta.
        /// </summary>
        private void UpdateImageScale(double scaleFactor)
        {
            totalImageScale *= scaleFactor;
            ApplyScale();
        }

        /// <summary>
        /// Applies the computed scale to the image control.
        /// </summary>
        private void ApplyScale()
        {
            ((CompositeTransform)FileImage.RenderTransform).ScaleX = totalImageScale;
            ((CompositeTransform)FileImage.RenderTransform).ScaleY = totalImageScale;
        }

        /// <summary>
        /// Updates the image position by applying the delta.
        /// Checks that the image does not leave empty space around its edges.
        /// </summary>
        private void UpdateImagePosition(Point delta)
        {
            var newPosition = new Point(imagePosition.X + delta.X, imagePosition.Y + delta.Y);

            if (newPosition.X > 0) newPosition.X = 0;
            if (newPosition.Y > 0) newPosition.Y = 0;

            if ((FileImage.ActualWidth * totalImageScale) + newPosition.X < FileImage.ActualWidth)
                newPosition.X = FileImage.ActualWidth - (FileImage.ActualWidth * totalImageScale);

            if ((FileImage.ActualHeight * totalImageScale) + newPosition.Y < FileImage.ActualHeight)
                newPosition.Y = FileImage.ActualHeight - (FileImage.ActualHeight * totalImageScale);

            imagePosition = newPosition;

            ApplyPosition();
        }

        /// <summary>
        /// Applies the computed position to the image control.
        /// </summary>
        private void ApplyPosition()
        {
            ((CompositeTransform)FileImage.RenderTransform).TranslateX = imagePosition.X;
            ((CompositeTransform)FileImage.RenderTransform).TranslateY = imagePosition.Y;
        }

        /// <summary>
        /// Resets the zoom to its original scale and position
        /// </summary>
        private void ResetImagePosition()
        {
            totalImageScale = 1;
            imagePosition.X = imagePosition.Y = 0;
            ApplyScale();
            ApplyPosition();
        }

        /// <summary>
        /// Checks that dragging by the given amount won't result in empty space around the image
        /// </summary>
        private bool IsDragValid(double scaleDelta, Point translateDelta)
        {
            if (imagePosition.X + translateDelta.X > 0 || imagePosition.Y + translateDelta.Y > 0)
                return false;
            if ((FileImage.ActualWidth * totalImageScale * scaleDelta) + (imagePosition.X + translateDelta.X) < FileImage.ActualWidth)
                return false;
            if ((FileImage.ActualHeight * totalImageScale * scaleDelta) + (imagePosition.Y + translateDelta.Y) < FileImage.ActualHeight)
                return false;
            return true;
        }

        #endregion

        #endregion
    }
}