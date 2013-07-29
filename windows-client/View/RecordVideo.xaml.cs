/* 
    Copyright (c) 2011 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://go.microsoft.com/fwlink/?LinkID=219604 
  
*/
using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Controls;
using System.Collections.Generic;
// Directives
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using System.Windows.Threading;
using windows_client.Languages;
using System.Windows.Media.Imaging;
using Windows.Phone.Media.Capture;
using System.Diagnostics;
using Microsoft.Devices;
using Windows.Storage;
using Windows.Storage.Streams;

namespace windows_client.View
{
    public partial class RecordVideo : PhoneApplicationPage
    {
        // Source and device for capturing video.
        AudioVideoCaptureDevice videoCaptureDevice;
        IRandomAccessStream videoStream;

        List<Windows.Foundation.Size> resolutions;
        Windows.Foundation.Size selectedResolution;
        
        IsolatedStorageFileStream isoVideoFile;
        // File details for storing the recording.        
        private string isoVideoFileName = HikeConstants.TEMP_VIDEO_NAME;
        private string path = String.Empty;

        private DispatcherTimer progressTimer;
        private readonly int maxVideoRecordTime = 30;
        private int maxPlayingTime;

        // For managing button and application state.
        private enum ButtonState { Initialized, Ready, Recording, Playback, Paused, NoChange, CameraNotSupported, SettingMenu };
        private ButtonState currentAppState;

        bool isPrimaryCam = true;
        private ApplicationBar appBar;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton doneIconButton = null;
        ApplicationBarIconButton recordIconButton = null;
        ApplicationBarIconButton settingIconButton = null;
        ApplicationBarIconButton playIconButton = null;
        ApplicationBarIconButton pauseIconButton = null;
        ApplicationBarIconButton stopIconButton = null;
        private int runningSeconds = -1;

        public RecordVideo()
        {
            InitializeComponent();
            InitAppBar();

            SetCameraDevices();
            SetResolutions();

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += new EventHandler(showProgress);
            maxPlayingTime = maxVideoRecordTime;

            TimeSpan ts = new TimeSpan(0, 0, maxVideoRecordTime);
            txtDebug.Text = ts.ToString("mm\\:ss");
        }

        private void InitAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            //add icon for send
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            sendIconButton.Text = AppResources.Send_Txt;
            sendIconButton.Click += new EventHandler(send_Click);
            sendIconButton.IsEnabled = true;

            playIconButton = new ApplicationBarIconButton();
            playIconButton.IconUri = new Uri("/View/images/appbar_icon_play.png", UriKind.Relative);
            playIconButton.Text = AppResources.Play_Txt;
            playIconButton.Click += new EventHandler(StartPlayback_Click);
            playIconButton.IsEnabled = true;

            pauseIconButton = new ApplicationBarIconButton();
            pauseIconButton.IconUri = new Uri("/View/images/icon_pause.png", UriKind.Relative);
            pauseIconButton.Text = AppResources.Pause_Txt;
            pauseIconButton.Click += new EventHandler(PausePlayback_Click);
            pauseIconButton.IsEnabled = true;

            stopIconButton = new ApplicationBarIconButton();
            stopIconButton.IconUri = new Uri("/View/images/icon_stop_appbar.png", UriKind.Relative);
            stopIconButton.Text = AppResources.Stop_Txt;
            stopIconButton.Click += new EventHandler(StopPlaybackRecording_Click);
            stopIconButton.IsEnabled = true;

            recordIconButton = new ApplicationBarIconButton();
            recordIconButton.IconUri = new Uri("/View/images/icon_record_appbar.png", UriKind.Relative);
            recordIconButton.Text = AppResources.Record_Txt;
            recordIconButton.Click += new EventHandler(StartRecording_Click);
            recordIconButton.IsEnabled = true;
            appBar.Buttons.Add(recordIconButton);

            settingIconButton = new ApplicationBarIconButton();
            settingIconButton.IconUri = new Uri("/View/images/icon_editprofile.png", UriKind.Relative);
            settingIconButton.Text = AppResources.Settings;
            settingIconButton.Click += settingsButton_Click;
            settingIconButton.IsEnabled = true;
            appBar.Buttons.Add(settingIconButton);

            doneIconButton = new ApplicationBarIconButton();
            doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            doneIconButton.Text = AppResources.OK;
            doneIconButton.Click += doneIconButton_Click;
            doneIconButton.IsEnabled = true;

            recordVideo.ApplicationBar = appBar;
        }

        private void SetResolutions()
        {
            try
            {
                var res = isPrimaryCam ? AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back) : AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Front);

                if (res != null)
                {
                    resolutions = res.ToList();

                    if (resolutions != null && resolutions.Count > 0)
                    {
                        selectedResolution = resolutions.First();


                        resolutionList.ItemsSource = resolutions;
                        resolutionList.SelectedItem = selectedResolution;
                    }
                }
            }
            catch
            {
                //resolutions are empty
            }
        }

        private void SetCameraDevices()
        {
            var camList = new List<string>();

            var cameraLocations = AudioVideoCaptureDevice.AvailableSensorLocations;

            if (cameraLocations != null && cameraLocations.Count > 0)
            {
                isPrimaryCam = cameraLocations.First().ToString().Contains("Back") ? true : false;

                foreach (var cam in cameraLocations)
                    camList.Add(cam.ToString());

                cameraList.ItemsSource = camList;
                cameraList.SelectedItem = camList.First();
            }
        }

        async void doneIconButton_Click(object sender, EventArgs e)
        {
            videoCaptureDevice.Dispose();
            videoCaptureDevice = isPrimaryCam ? await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Back, selectedResolution) : await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Front, selectedResolution);

            if (isPrimaryCam)
            {
                playerTransform.ScaleX = 1;
                viewfinderTransform.ScaleX = 1;
            }
            else
            {
                playerTransform.ScaleX = -1;
                viewfinderTransform.ScaleX = -1;
            }

            videoRecorderBrush.SetSource(videoCaptureDevice);

            SettingsGrid.Visibility = Visibility.Collapsed;
            if (runningSeconds <= 0 && String.IsNullOrEmpty(txtSize.Text))
            {
                UpdateUI(ButtonState.Initialized);
            }
            else
            {
                addOrRemoveAppBarButton(sendIconButton, true);
                UpdateUI(ButtonState.Ready);
            }
        }

        void settingsButton_Click(object sender, EventArgs e)
        {
            UpdateUI(ButtonState.SettingMenu);
            SettingsGrid.Visibility = Visibility.Visible;
        }

        private void send_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_RECORDED] = thumbnail;
            NavigationService.GoBack();
        }

        private void addOrRemoveAppBarButton(ApplicationBarIconButton iconButton, bool isAdd)
        {
            if (isAdd)
            {
                if (!this.appBar.Buttons.Contains(iconButton) && this.appBar.Buttons.Count < 4)
                    this.appBar.Buttons.Add(iconButton);
            }
            else
            {
                if (this.appBar.Buttons.Contains(iconButton))
                    this.appBar.Buttons.Remove(iconButton);
            }
        }

        void showProgress(object sender, EventArgs e)
        {
            updateProgress();
        }

        private void updateProgress()
        {
            ++runningSeconds;
            TimeSpan ts = new TimeSpan(0, 0, maxPlayingTime - runningSeconds);
            txtDebug.Text = ts.ToString("mm\\:ss");
            
            if (videoStream != null && currentAppState == ButtonState.Recording)
                txtSize.Text = ConvertToStorageSizeString(videoStream.Size);

            if (runningSeconds == maxPlayingTime)
            {
                StopVideoRecording();
            }
        }

        string ConvertToStorageSizeString(ulong size)
        {
            var value = Convert.ToInt64(size);

            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int i = 0;
            decimal dValue = (decimal)value;

            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n1} {1}", dValue, suffixes[i]);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Initialize the video recorder.
            InitializeVideoRecorder();
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            progressTimer.Stop();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Dispose of camera and media objects.
            DisposeVideoPlayer();
            DisposeVideoRecorder();

            base.OnNavigatedFrom(e);
        }

        // Update the buttons and text on the UI thread based on app state.
        private void UpdateUI(ButtonState currentButtonState)
        {
            // Run code on the UI thread.
            Dispatcher.BeginInvoke(delegate
            {
                switch (currentButtonState)
                {
                    // When the camera is not supported by the device.
                    case ButtonState.CameraNotSupported:
                        recordIconButton.IsEnabled = false;
                        settingIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;
                        doneIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    // First launch of the application, so no video is available.
                    case ButtonState.Initialized:
                        recordIconButton.IsEnabled = true;
                        settingIconButton.IsEnabled = true;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;
                        doneIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, true);
                        addOrRemoveAppBarButton(settingIconButton, true);

                        break;

                    // Ready to record, so video is available for viewing.
                    case ButtonState.Ready:
                        recordIconButton.IsEnabled = true;
                        settingIconButton.IsEnabled = true;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = true;
                        pauseIconButton.IsEnabled = false;
                        doneIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, true);
                        addOrRemoveAppBarButton(settingIconButton, true);
                        addOrRemoveAppBarButton(playIconButton, true);

                        break;

                    // Video recording is in progress.
                    case ButtonState.Recording:
                        recordIconButton.IsEnabled = false;
                        settingIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = true;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;
                        doneIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);

                        break;

                    // Video playback is in progress.
                    case ButtonState.Playback:
                        recordIconButton.IsEnabled = false;
                        settingIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = true;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = true;
                        doneIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, true);
                        addOrRemoveAppBarButton(stopIconButton, true);

                        break;

                    // Video playback has been paused.
                    case ButtonState.Paused:
                        recordIconButton.IsEnabled = false;
                        settingIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = true;
                        playIconButton.IsEnabled = true;
                        pauseIconButton.IsEnabled = false;
                        doneIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);
                        addOrRemoveAppBarButton(playIconButton, true);

                        break;

                    case ButtonState.SettingMenu:
                        recordIconButton.IsEnabled = false;
                        settingIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;
                        doneIconButton.IsEnabled = true;

                        addOrRemoveAppBarButton(sendIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(doneIconButton, true);
                        break;
                    default:
                        break;
                }

                // Display a message.

                // Note the current application state.
                currentAppState = currentButtonState;
            });
        }

        private byte[] thumbnail = null;

        public async void InitializeVideoRecorder()
        {
            try
            {
                if (videoCaptureDevice == null)
                {
                    videoCaptureDevice = isPrimaryCam ? await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Back, selectedResolution) : await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Front, selectedResolution);

                    videoCaptureDevice.PreviewFrameAvailable += videoCaptureDevice_PreviewFrameAvailable;
                    // Initialize the camera if it exists on the device.
                    if (videoCaptureDevice != null)
                    {
                        // Create the VideoBrush for the viewfinder.
                        videoRecorderBrush.SetSource(videoCaptureDevice);

                        // Set the button state and the message.
                        UpdateUI(ButtonState.Initialized);
                    }
                    else
                    {
                        // Disable buttons when the camera is not supported by the device.
                        UpdateUI(ButtonState.CameraNotSupported);
                    }
                }
            }
            catch
            {
                UpdateUI(ButtonState.CameraNotSupported);
            }
        }

        void videoCaptureDevice_PreviewFrameAvailable(ICameraCaptureDevice sender, object args)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (runningSeconds > 0 && thumbnail == null)
                    {
                        int frameWidth = (int)this.videoCaptureDevice.PreviewResolution.Width;
                        int frameHeight = (int)this.videoCaptureDevice.PreviewResolution.Height;

                        var argbArray = new int[frameWidth * frameHeight];
                        this.videoCaptureDevice.GetPreviewBufferArgb(argbArray);

                        var wb = new WriteableBitmap(frameWidth, frameHeight);
                        argbArray.CopyTo(wb.Pixels, 0);
                        MemoryStream stream = new MemoryStream();
                        wb.SaveJpeg(stream, HikeConstants.ATTACHMENT_THUMBNAIL_MAX_WIDTH, HikeConstants.ATTACHMENT_THUMBNAIL_MAX_HEIGHT, 0, 60);
                        thumbnail = stream.ToArray();
                    }
                });
        }

        private async void StartVideoRecording()
        {
            try
            {
                txtDebug.Opacity = 1;
                runningSeconds = -1;
                maxPlayingTime = maxVideoRecordTime;
                progressTimer.Start();
                updateProgress();

                try
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFile storageFile = await localFolder.CreateFileAsync(isoVideoFileName, CreationCollisionOption.ReplaceExisting);
                    path = storageFile.Path;
                    if (storageFile != null)
                    {
                        videoStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
                        await videoCaptureDevice.FocusAsync();
                        await videoCaptureDevice.StartRecordingToStreamAsync(videoStream);
                    }
                }
                catch (FileNotFoundException)
                {
                }

                // Set the button states and the message.
                UpdateUI(ButtonState.Recording);
            }

            // If recording fails, display an error.
            catch (Exception e)
            {
                Debug.WriteLine("RecoedVideo.xaml :: StartVideoRecording, Exception : " + e.StackTrace);
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "ERROR: " + e.Message.ToString();
                });
            }
        }

        // Set the recording state: stop recording.
        private async void StopVideoRecording()
        {
            try
            {
                stopIconButton.IsEnabled = false;
                addOrRemoveAppBarButton(stopIconButton, false);
                addOrRemoveAppBarButton(sendIconButton, true);
                txtDebug.Opacity = 0;
                maxPlayingTime = runningSeconds;
                runningSeconds = -1;
                progressTimer.Stop();
                updateProgress();

                if (currentAppState == ButtonState.Recording)
                {
                    await videoCaptureDevice.StopRecordingAsync();
                    videoStream.AsStream().Dispose();
                    UpdateUI(ButtonState.NoChange);
                    StartVideoPreview();
                }
            }
            // If stop fails, display an error.
            catch (Exception e)
            {
                Debug.WriteLine("RecoedVideo.xaml :: StopVideoRecording, Exception : " + e.StackTrace);
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "ERROR: " + e.Message.ToString();
                });
            }
        }

        private void StartVideoPreview()
        {
            try
            {
                videoRecorderBrush.SetSource(videoCaptureDevice);

                viewfinderRectangle.Fill = videoRecorderBrush;

                UpdateUI(ButtonState.Ready);
            }
            // If preview fails, display an error.
            catch (Exception e)
            {
            }
        }

        // Start the video recording.
        private void StartRecording_Click(object sender, EventArgs e)
        {
            try
            {
                // Avoid duplicate taps.
                recordIconButton.IsEnabled = false;
                addOrRemoveAppBarButton(recordIconButton, false);
                addOrRemoveAppBarButton(sendIconButton, false);
                //captureSource.CaptureImageAsync();
                progressTimer.Start();
                StartVideoRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Record Video :: StartRecording_Click , Exception:" + ex.StackTrace);
            }
        }

        // Handle stop requests.
        private void StopPlaybackRecording_Click(object sender, EventArgs e)
        {
            // Avoid duplicate taps.
            txtDebug.Opacity = 0;
            progressTimer.Stop();
            updateProgress();

            // Stop during video recording.
            if (currentAppState == ButtonState.Recording)
            {
                StopVideoRecording();
                // Set the button state and the message.
                UpdateUI(ButtonState.NoChange);
            }

            // Stop during video playback.
            else
            {
                runningSeconds = -1;
                // Remove playback objects.
                DisposeVideoPlayer();
                StartVideoPreview();
                // Set the button state and the message.
                UpdateUI(ButtonState.NoChange);
            }
        }

        // Start video playback.
        private void StartPlayback_Click(object sender, EventArgs e)
        {
            txtDebug.Opacity = 1;
            progressTimer.Start();

            // Avoid duplicate taps.
            playIconButton.IsEnabled = false;
            addOrRemoveAppBarButton(playIconButton, false);
            // Start video playback when the file stream exists.

            if (isoVideoFile != null && VideoPlayer.Source != null)
            {
                VideoPlayer.Play();
            }
            // Start the video for the first time.
            else
            {
                // Remove VideoBrush from the tree.
                viewfinderRectangle.Fill = null;

                // Create the file stream and attach it to the MediaElement.
                isoVideoFile = new IsolatedStorageFileStream(isoVideoFileName, FileMode.Open, FileAccess.Read, IsolatedStorageFile.GetUserStoreForApplication());

                VideoPlayer.SetSource(isoVideoFile);

                // Add an event handler for the end of playback.
                VideoPlayer.MediaEnded += new RoutedEventHandler(VideoPlayerMediaEnded);

                // Start video playback.
                VideoPlayer.Play();
            }

            // Set the button state and the message.
            UpdateUI(ButtonState.Playback);
        }

        // Pause video playback.
        private void PausePlayback_Click(object sender, EventArgs e)
        {
            progressTimer.Stop();
            // Avoid duplicate taps.
            pauseIconButton.IsEnabled = false;
            addOrRemoveAppBarButton(pauseIconButton, false);

            // If mediaElement exists, pause playback.
            if (VideoPlayer != null)
            {
                VideoPlayer.Pause();
            }

            // Set the button state and the message.
            UpdateUI(ButtonState.Paused);
        }

        private void DisposeVideoPlayer()
        {
            if (VideoPlayer != null)
            {
                // Stop the VideoPlayer MediaElement.
                VideoPlayer.Stop();

                // Remove playback objects.
                VideoPlayer.Source = null;

                // Remove the event handler.
                VideoPlayer.MediaEnded -= VideoPlayerMediaEnded;
            }
        }

        private void DisposeVideoRecorder()
        {
            if (videoCaptureDevice != null)
                videoCaptureDevice.Dispose();

            videoCaptureDevice = null;
        }

        // If recording fails, display an error message.
        private void OnCaptureFailed(object sender, ExceptionRoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                txtDebug.Text = "ERROR: " + e.ErrorException.Message.ToString();
            });
        }

        // Display the viewfinder when playback ends.
        public void VideoPlayerMediaEnded(object sender, RoutedEventArgs e)
        {
            // Remove the playback objects.
            DisposeVideoPlayer();
            StartVideoPreview();
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (e.Orientation == PageOrientation.LandscapeLeft)
                base.OnOrientationChanged(e);
        }

        private void cameraList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Collapsed)
                return;

            var list = sender as ListBox;
            if (list != null && list.SelectedItem != null)
            {
                var devices = AudioVideoCaptureDevice.AvailableSensorLocations;
                var camName = list.SelectedItem as String;
      
                foreach (var device in devices)
                {
                    if (device.ToString() == camName)
                    {
                        if (camName.Contains("Back"))
                            isPrimaryCam = true;
                        else
                            isPrimaryCam = false;
                        
                        break;
                    }
                }

                SetResolutions();
            }
        }

        private void resolutionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Collapsed)
                return;

            var list = sender as ListBox;
            if (list != null && list.SelectedItem != null)
                selectedResolution = (Windows.Foundation.Size) list.SelectedItem;
        }
    }
}
