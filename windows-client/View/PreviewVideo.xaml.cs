using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;
using Microsoft.Phone.Tasks;
using System.Windows.Resources;
using System.Diagnostics;
using windows_client.Languages;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace windows_client.View
{
    public partial class PreviewVideo : PhoneApplicationPage
    {
        public PreviewVideo()
        {
            InitializeComponent();
            this.ApplicationBar = new ApplicationBar();

            ApplicationBarIconButton shareVideo = new ApplicationBarIconButton();
            shareVideo.Text = "share";
            shareVideo.IconUri = new Uri("/View/images/AppBar/icon_send.png", UriKind.RelativeOrAbsolute); ;
            shareVideo.Click += shareVideo_Click;

            this.ApplicationBar.Buttons.Add(shareVideo);
            //Stream stream = new MemoryStream((byte[])PhoneApplicationService.Current.State[HikeConstants.VIDEO_THUMB_SHARED]);
            
            //StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative));
            //byte[] videoBytes = AccountUtils.StreamToByteArray(streamInfo.Stream);
            //Stream stream = new MemoryStream(videoBytes);
            //WriteableBitmap wb = CreateThumbnail(stream,600,600);
            //BitmapImage thumbImage = new BitmapImage();
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    wb.SaveJpeg(ms, 600, 600, 0, 100);
            //    thumbImage.SetSource(ms);
            //}
            //thumbnailImage.Source = thumbImage;
            //var video = new MediaItem(filePath);
            //using (var bitmap = video.MainMediaFile.GetThumbnail(
            //    new TimeSpan(0, 0, 5),
            //    new System.Drawing.Size(640, 480)))
            //{
            //    // do something with the bitmap like:
            //    bitmap.Save("thumb1.jpg");
            //}
            ////createThumbnailMethod();
            thumbnailImage.Source = UI_Utils.Instance.createImageFromBytes((byte[])PhoneApplicationService.Current.State[HikeConstants.VIDEO_THUMB_SHARED]);
            VideoDurationText.Text = TimeUtils.GetProperTimeFromMilliseconds((int)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED_DURATION]);
            //double videoSize = (int)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED_SIZE];
            //videoSize = videoSize / (1024 * 1024);
            //videoSize = Math.Round(videoSize, 1);
            //VideoSizeText.Text = videoSize.ToString();
        }

        void shareVideo_Click(object sender, EventArgs e)
        {
            int videoSize = (int)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED_SIZE];
            if (videoSize > HikeConstants.FILE_MAX_SIZE)
            {
                //int MaxFileSizeInMB = HikeConstants.FILE_MAX_SIZE / (1024 * 1024 * 10);
                MessageBox.Show(AppResources.CT_FileSizeExceed_Text, AppResources.CT_FileSizeExceed_Caption_Text, MessageBoxButton.OK);
                //return;
                //var result = MessageBox.Show("Your video is larger than " + MaxFileSizeInMB + "MB. Send first " + MaxFileSizeInMB + "MB.", "Video Size Exceeds Maximum Limit", MessageBoxButton.OKCancel);
                //if (result == MessageBoxResult.OK)
                //{
                //    try
                //    {
                //        StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative));
                //        byte[] videoBytes = AccountUtils.InitialBytesStreamToByteArray(streamInfo.Stream, HikeConstants.FILE_MAX_SIZE/(2*5));
                //        PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;
                //        Debug.WriteLine(videoBytes.Length);
                //    }
                //    catch (Exception ex)
                //    {
                //        Debug.WriteLine(ex.Message);
                //    }
                //    if (NavigationService.CanGoBack)
                //        NavigationService.RemoveBackEntry();
                //    if (NavigationService.CanGoBack)
                //        NavigationService.GoBack();
                //}
                //else
                //{
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_THUMB_SHARED);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED_DURATION);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED_SIZE);
                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack();
                    //NavigationService.Navigate(new Uri("/View/ViewVideos.xaml", UriKind.Relative));
               // }
            }
            else
            {
                //try
                //{
                //    StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative));
                //    byte[] videoBytes = AccountUtils.StreamToByteArray(streamInfo.Stream);
                //    PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //}
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        private void ContentPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Utils.PlayFileInMediaPlayer((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED]);
                //MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                //mediaPlayerLauncher.Media = new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative);
                //mediaPlayerLauncher.Location = MediaLocationType.Data;
                //mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                //mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
                //try
                //{
                //    mediaPlayerLauncher.Show();
                //}
                //catch (Exception ex)
                //{
                //}
        }
        //private async void createThumbnailMethod()
        //{
        //    try
        //    {
        //        var files = await KnownFolders.CameraRoll.GetFilesAsync(); // Throw UnauthorizedAccessException without ID_CAP_MEDIALIB_PHOTO_FULL
        //        foreach (var item in files)
        //        {
        //            StorageFile storageFile = item;
        //            Debug.WriteLine(storageFile.Name);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }
        //}

        //private WriteableBitmap CreateThumbnail(Stream stream, int width, int height)
        //{
        //    // This hack comes from the problem that classes like BitmapImage, WritableBitmap, Image used here could 
        //    // only be created or accessed from the UI thread. And now this code called from the threadpool. To avoid
        //    // cross-thread access exceptions, I dispatch the code back to the UI thread, waiting for it to complete
        //    // using the Monitor and a lock object, and then return the value from the method. Quite hacky, but the only
        //    // way to make this work currently. It's quite stupid that MS didn't provide any classes to do image 
        //    // processing on the non-UI threads.
        //    WriteableBitmap result = null;
            
        //    var bi = new BitmapImage();
        //    try
        //    {
        //        bi.SetSource(stream);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }
        //    int w, h;
        //    double ws = (double)width / bi.PixelWidth;
        //    double hs = (double)height / bi.PixelHeight;
        //    double scale = (ws > hs) ? ws : hs;
        //    w = (int)(bi.PixelWidth * scale);
        //    h = (int)(bi.PixelHeight * scale);

        //    var im = new Image();
        //    im.Stretch = Stretch.UniformToFill;
        //    im.Source = bi;

        //    result = new WriteableBitmap(width, height);
        //    var tr = new CompositeTransform();
        //    tr.CenterX = (ws > hs) ? 0 : (width - w) / 2;
        //    tr.CenterY = (ws < hs) ? 0 : (height - h) / 2;
        //    tr.ScaleX = scale;
        //    tr.ScaleY = scale;
        //    result.Render(im, tr);
        //    result.Invalidate();
                        
        //    return result;
        //}
    }
}