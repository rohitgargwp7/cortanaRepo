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
using System.Diagnostics;

namespace windows_client.View
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Source and device for capturing video.
        private CaptureSource captureSource;
        private VideoCaptureDevice videoCaptureDevice;

        // File details for storing the recording.        
        private IsolatedStorageFileStream isoVideoFile;
        private FileSink fileSink;
        private string isoVideoFileName = HikeConstants.TEMP_VIDEO_NAME;

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


        public MainPage()
        {
            InitializeComponent();
            initAppBar();

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += new EventHandler(showProgress);
            maxPlayingTime = maxVideoRecordTime;

            TimeSpan ts = new TimeSpan(0, 0, maxVideoRecordTime);
            txtDebug.Text = ts.ToString("mm\\:ss");
        }

        private void initAppBar()
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
            appBar.Buttons.Add(pauseIconButton);

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
            settingIconButton.IconUri = new Uri("/Vieew/images/icon_setting.png", UriKind.Relative);
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

        void doneIconButton_Click(object sender, EventArgs e)
        {
            SettingsGrid.Visibility = Visibility.Collapsed;
            if (runningSeconds <= 0)
            {
                UpdateUI(ButtonState.Initialized);
                captureSource.Start();
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
            captureSource.Stop();
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
            if (runningSeconds == maxPlayingTime)
            {
                StopVideoRecording();
            }
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

        void captureSource_CaptureImageCompleted(object sender, CaptureImageCompletedEventArgs e)
        {
            using (var msLargeImage = new MemoryStream())
            {
                e.Result.SaveJpeg(msLargeImage, HikeConstants.ATTACHMENT_THUMBNAIL_MAX_WIDTH,
                    HikeConstants.ATTACHMENT_THUMBNAIL_MAX_HEIGHT, 0, 60);
                thumbnail = msLargeImage.ToArray();
            }
        }

        public void InitializeVideoRecorder()
        {
            if (captureSource == null)
            {
                // Create the VideoRecorder objects.
                captureSource = new CaptureSource();
                fileSink = new FileSink();

                SetCameraDevices();
                SetResolutions();

                captureSource.VideoCaptureDevice = videoCaptureDevice;
                captureSource.CaptureImageCompleted += captureSource_CaptureImageCompleted;
                // Add eventhandlers for captureSource.
                captureSource.CaptureFailed += new EventHandler<ExceptionRoutedEventArgs>(OnCaptureFailed);
                // Initialize the camera if it exists on the device.
                if (videoCaptureDevice != null)
                {
                    // Create the VideoBrush for the viewfinder.
                    videoRecorderBrush.SetSource(captureSource);

                    // Start video capture and display it on the viewfinder.
                    captureSource.Start();

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

        private void SetResolutions()
        {
            List<String> resList = new List<string>();

            foreach (var format in videoCaptureDevice.SupportedFormats)
                if (format.PixelFormat == PixelFormatType.Format32bppArgb)
                {
                    if (videoCaptureDevice.DesiredFormat == null)
                        videoCaptureDevice.DesiredFormat = format;

                    resList.Add(format.PixelHeight.ToString());
                }

            resolutionList.ItemsSource = resList;
            resolutionList.SelectedItem = resList.First();
        }

        private void SetCameraDevices()
        {
            List<String> camList = new List<string>();
            var devices = CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices();

            foreach (var device in devices)
            {
                camList.Add(device.FriendlyName);

                if (device.FriendlyName.Contains("Primary"))
                {
                    isPrimaryCam = true;
                    videoCaptureDevice = device;
                }
            }

            cameraList.ItemsSource = camList;
            cameraList.SelectedItem = camList.Where(d => d.Contains("Primary")).First();
        }


        // Set recording state: start recording.
        private void StartVideoRecording()
        {
            try
            {
                txtDebug.Opacity = 1;
                runningSeconds = -1;
                maxPlayingTime = maxVideoRecordTime;
                progressTimer.Start();
                updateProgress();

                // Connect fileSink to captureSource.
                if (captureSource.VideoCaptureDevice != null
                    && captureSource.State == CaptureState.Started)
                {
                    captureSource.Stop();
                    // Connect the input and output of fileSink.
                    fileSink.CaptureSource = captureSource;
                    fileSink.IsolatedStorageFileName = isoVideoFileName;
                }

                // Begin recording.
                if (captureSource.VideoCaptureDevice != null
                    && captureSource.State == CaptureState.Stopped)
                {
                    captureSource.Start();
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
        private void StopVideoRecording()
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

                // Stop recording.
                if (captureSource.VideoCaptureDevice != null
                && captureSource.State == CaptureState.Started)
                {
                    captureSource.Stop();
                    // Disconnect fileSink.
                    fileSink.CaptureSource = null;
                    fileSink.IsolatedStorageFileName = null;
                    // Set the button states and the message.
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

        // Set the recording state: display the video on the viewfinder.
        private void StartVideoPreview()
        {
            try
            {
                // Display the video on the viewfinder.
                if (captureSource.VideoCaptureDevice != null
                && captureSource.State == CaptureState.Stopped)
                {
                    // Add captureSource to videoBrush.
                    videoRecorderBrush.SetSource(captureSource);

                    captureSource.Start();

                    // Set the button states and the message.
                    UpdateUI(ButtonState.Ready);
                }
            }
            // If preview fails, display an error.
            catch (Exception e)
            {
                Debug.WriteLine("RecoedVideo.xaml :: StartVideoPreview, Exception : " + e.StackTrace);
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "ERROR: " + e.Message.ToString();
                });
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
                captureSource.CaptureImageAsync();
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
            if (isoVideoFile != null)
            {
                VideoPlayer.Play();
            }
            // Start the video for the first time.
            else
            {
                // Stop the capture source.
                captureSource.Stop();

                // Remove VideoBrush from the tree.
                viewfinderRectangle.Fill = null;

                // Create the file stream and attach it to the MediaElement.
                isoVideoFile = new IsolatedStorageFileStream(isoVideoFileName,
                                        FileMode.Open, FileAccess.Read,
                                        IsolatedStorageFile.GetUserStoreForApplication());

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
                isoVideoFile = null;

                // Remove the event handler.
                VideoPlayer.MediaEnded -= VideoPlayerMediaEnded;
            }
        }

        private void DisposeVideoRecorder()
        {
            if (captureSource != null)
            {
                // Stop captureSource if it is running.
                if (captureSource.VideoCaptureDevice != null
                    && captureSource.State == CaptureState.Started)
                {
                    captureSource.Stop();
                }

                // Remove the event handlers for capturesource and the shutter button.
                captureSource.CaptureFailed -= OnCaptureFailed;

                // Remove the video recording objects.
                captureSource = null;
                videoCaptureDevice = null;
                fileSink = null;
            }
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
            if (isPrimaryCam)
            {
                if (e.Orientation == PageOrientation.LandscapeLeft)
                    base.OnOrientationChanged(e);
            }
            else
            {
                if ((e.Orientation & PageOrientation.Portrait) == PageOrientation.Portrait)
                {
                   
                }
            }
        }

        private void cameraList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Collapsed)
                return;

            var list = sender as ListBox;
            if (list != null && list.SelectedItem != null)
            {
                var devices = CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices();
                var camName = list.SelectedItem as String;
      
                foreach (var device in devices)
                {
                    if (device.FriendlyName == camName)
                    {
                        if (camName.Contains("Primary"))
                            isPrimaryCam = true;
                        else
                        {
                            isPrimaryCam = false;
                            viewfinderTransform.Rotation = 270;
                        }
                        
                        videoCaptureDevice = device;
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
            {
                var res = list.SelectedItem as String;
                var height = Convert.ToInt32(res);

                VideoFormat newFormat = videoCaptureDevice.SupportedFormats.First();

                foreach(var format in videoCaptureDevice.SupportedFormats)
                    if(format.PixelFormat == PixelFormatType.Format32bppArgb && format.PixelHeight == height)
                        newFormat = format;

                videoCaptureDevice.DesiredFormat = newFormat;

                videoRecorderBrush.SetSource(captureSource);

                captureSource.VideoCaptureDevice = videoCaptureDevice;
            }
        }
    }
}
