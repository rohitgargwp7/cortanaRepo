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

// Directives
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using System.Windows.Threading;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Viewfinder for capturing video.
        private VideoBrush videoRecorderBrush;

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
        private enum ButtonState { Initialized, Ready, Recording, Playback, Paused, NoChange, CameraNotSupported };
        private ButtonState currentAppState;


        private ApplicationBar appBar;
        ApplicationBarMenuItem blockUnblockMenuItem;
        ApplicationBarMenuItem muteGroupMenuItem;
        ApplicationBarMenuItem inviteMenuItem = null;
        ApplicationBarMenuItem addToFavMenuItem = null;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton recordIconButton = null;
        ApplicationBarIconButton playIconButton = null;
        ApplicationBarIconButton pauseIconButton = null;
        ApplicationBarIconButton stopIconButton = null;
        private int runningSeconds = 0;


        public MainPage()
        {
            InitializeComponent();
            initAppBar();

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += new EventHandler(showProgress);
            maxPlayingTime = maxVideoRecordTime;
        }

        private void initAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            //add icon for send
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            sendIconButton.Text = AppResources.Send_Txt;
            sendIconButton.Click += new EventHandler(send_Click);
            sendIconButton.IsEnabled = true;
            //            appBar.Buttons.Add(sendIconButton);

            playIconButton = new ApplicationBarIconButton();
            playIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            playIconButton.Text = AppResources.Play_Txt;
            playIconButton.Click += new EventHandler(StartPlayback_Click);
            playIconButton.IsEnabled = true;
            //            appBar.Buttons.Add(playIconButton);

            pauseIconButton = new ApplicationBarIconButton();
            pauseIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            pauseIconButton.Text = AppResources.Pause_Txt;
            pauseIconButton.Click += new EventHandler(PausePlayback_Click);
            pauseIconButton.IsEnabled = true;
            appBar.Buttons.Add(pauseIconButton);

            stopIconButton = new ApplicationBarIconButton();
            stopIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            stopIconButton.Text = AppResources.Stop_Txt;
            stopIconButton.Click += new EventHandler(StopPlaybackRecording_Click);
            stopIconButton.IsEnabled = true;
            //            appBar.Buttons.Add(stopIconButton);

            recordIconButton = new ApplicationBarIconButton();
            recordIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            recordIconButton.Text = AppResources.Record_Txt;
            recordIconButton.Click += new EventHandler(StartRecording_Click);
            recordIconButton.IsEnabled = true;
            appBar.Buttons.Add(recordIconButton);
            recordVideo.ApplicationBar = appBar;
        }

        private void send_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_RECORDED] = "Capture";
            NavigationService.GoBack();
        }

        private void addOrRemoveAppBarButton(ApplicationBarIconButton iconButton, bool isAdd)
        {
            if (isAdd)
            {
                if (!this.appBar.Buttons.Contains(iconButton))
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
            txtDebug.Text = Convert.ToString(++runningSeconds) + " : " + Convert.ToString(maxPlayingTime);
            recordProgress.Value = (double)runningSeconds / maxPlayingTime;
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
        private void UpdateUI(ButtonState currentButtonState, string statusMessage)
        {
            // Run code on the UI thread.
            Dispatcher.BeginInvoke(delegate
            {

                switch (currentButtonState)
                {
                    // When the camera is not supported by the device.
                    case ButtonState.CameraNotSupported:
                        recordIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    // First launch of the application, so no video is available.
                    case ButtonState.Initialized:
                        recordIconButton.IsEnabled = true;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(recordIconButton, true);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    // Ready to record, so video is available for viewing.
                    case ButtonState.Ready:
                        recordIconButton.IsEnabled = true;
                        stopIconButton.IsEnabled = false;
                        playIconButton.IsEnabled = true;
                        pauseIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(recordIconButton, true);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(playIconButton, true);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    // Video recording is in progress.
                    case ButtonState.Recording:
                        recordIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = true;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    // Video playback is in progress.
                    case ButtonState.Playback:
                        recordIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = true;
                        playIconButton.IsEnabled = false;
                        pauseIconButton.IsEnabled = true;

                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);
                        addOrRemoveAppBarButton(playIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, true);

                        break;

                    // Video playback has been paused.
                    case ButtonState.Paused:
                        recordIconButton.IsEnabled = false;
                        stopIconButton.IsEnabled = true;
                        playIconButton.IsEnabled = true;
                        pauseIconButton.IsEnabled = false;

                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);
                        addOrRemoveAppBarButton(playIconButton, true);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    default:
                        break;
                }

                // Display a message.

                // Note the current application state.
                currentAppState = currentButtonState;
            });
        }

        public void InitializeVideoRecorder()
        {
            if (captureSource == null)
            {
                // Create the VideoRecorder objects.
                captureSource = new CaptureSource();
                fileSink = new FileSink();

                videoCaptureDevice = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();

                // Add eventhandlers for captureSource.
                captureSource.CaptureFailed += new EventHandler<ExceptionRoutedEventArgs>(OnCaptureFailed);

                // Initialize the camera if it exists on the device.
                if (videoCaptureDevice != null)
                {
                    // Create the VideoBrush for the viewfinder.
                    videoRecorderBrush = new VideoBrush();
                    videoRecorderBrush.SetSource(captureSource);

                    // Display the viewfinder image on the rectangle.
                    viewfinderRectangle.Fill = videoRecorderBrush;

                    // Start video capture and display it on the viewfinder.
                    captureSource.Start();

                    // Set the button state and the message.
                    UpdateUI(ButtonState.Initialized, "Tap record to start recording...");
                }
                else
                {
                    // Disable buttons when the camera is not supported by the device.
                    UpdateUI(ButtonState.CameraNotSupported, "A camera is not supported on this device.");
                }
            }
        }

        // Set recording state: start recording.
        private void StartVideoRecording()
        {
            try
            {
                txtDebug.Opacity = 1;
                runningSeconds = 0;
                maxPlayingTime = maxVideoRecordTime;
                recordProgress.Opacity = 1;
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
                UpdateUI(ButtonState.Recording, "Recording...");
            }

            // If recording fails, display an error.
            catch (Exception e)
            {
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
                recordProgress.Opacity = 0;
                recordProgress.Value = 0;
                recordProgress.Opacity = 0;
                maxPlayingTime = runningSeconds;
                runningSeconds = 0;
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
                    UpdateUI(ButtonState.NoChange, "Preparing viewfinder...");

                    StartVideoPreview();
                }
            }
            // If stop fails, display an error.
            catch (Exception e)
            {
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

                    // Add videoBrush to the visual tree.
                    viewfinderRectangle.Fill = videoRecorderBrush;

                    captureSource.Start();

                    // Set the button states and the message.
                    UpdateUI(ButtonState.Ready, "Ready to record.");
                }
            }
            // If preview fails, display an error.
            catch (Exception e)
            {
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "ERROR: " + e.Message.ToString();
                });
            }
        }

        // Start the video recording.
        private void StartRecording_Click(object sender, EventArgs e)
        {
            // Avoid duplicate taps.
            recordIconButton.IsEnabled = false;
            addOrRemoveAppBarButton(recordIconButton, false);

            addOrRemoveAppBarButton(sendIconButton, false);

            progressTimer.Start();
            StartVideoRecording();
        }

        // Handle stop requests.
        private void StopPlaybackRecording_Click(object sender, EventArgs e)
        {
            // Avoid duplicate taps.
            txtDebug.Opacity = 0;
            recordProgress.Opacity = 0;
            progressTimer.Stop();
            updateProgress();

            // Stop during video recording.
            if (currentAppState == ButtonState.Recording)
            {
                StopVideoRecording();
                // Set the button state and the message.
                UpdateUI(ButtonState.NoChange, "Recording stopped.");
            }

            // Stop during video playback.
            else
            {
                runningSeconds = 0;
                // Remove playback objects.
                DisposeVideoPlayer();

                StartVideoPreview();

                // Set the button state and the message.
                UpdateUI(ButtonState.NoChange, "Playback stopped.");
            }
        }

        // Start video playback.
        private void StartPlayback_Click(object sender, EventArgs e)
        {
            txtDebug.Opacity = 1;
            progressTimer.Start();
            recordProgress.Opacity = 1;

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
            UpdateUI(ButtonState.Playback, "Playback started.");
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
            UpdateUI(ButtonState.Paused, "Playback paused.");
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
                videoRecorderBrush = null;
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
    }
}
