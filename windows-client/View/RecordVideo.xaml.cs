using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Controls;
using System.Collections.Generic;
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
using windows_client.DbUtils;
using System.Threading.Tasks;

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
        byte[] _snapshotByte;

        bool isPrimaryCam = true;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton doneIconButton = null;
        ApplicationBarIconButton recordIconButton = null;
        ApplicationBarIconButton settingIconButton = null;
        ApplicationBarIconButton pauseIconButton = null;
        ApplicationBarIconButton stopIconButton = null;
        private int runningSeconds = -1;

        public RecordVideo()
        {
            InitializeComponent();

            InitAppBar();

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += new EventHandler(showProgress);
            maxPlayingTime = maxVideoRecordTime;

            TimeSpan ts = new TimeSpan(0, 0, maxVideoRecordTime);
            txtDebug.Text = ts.ToString("mm\\:ss");
        }

        private void InitAppBar()
        {
            //add icon for send
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            sendIconButton.Text = AppResources.Send_Txt;
            sendIconButton.Click += new EventHandler(send_Click);

            pauseIconButton = new ApplicationBarIconButton();
            pauseIconButton.IconUri = new Uri("/View/images/icon_pause.png", UriKind.Relative);
            pauseIconButton.Text = AppResources.Pause_Txt;
            pauseIconButton.Click += new EventHandler(PausePlayback_Click);

            stopIconButton = new ApplicationBarIconButton();
            stopIconButton.IconUri = new Uri("/View/images/icon_stop_appbar.png", UriKind.Relative);
            stopIconButton.Text = AppResources.Stop_Txt;
            stopIconButton.Click += new EventHandler(StopPlaybackRecording_Click);
            
            recordIconButton = new ApplicationBarIconButton();
            recordIconButton.IconUri = new Uri("/View/images/icon_record_appbar.png", UriKind.Relative);
            recordIconButton.Text = AppResources.Record_Txt;
            recordIconButton.Click += new EventHandler(StartRecording_Click);

            settingIconButton = new ApplicationBarIconButton();
            settingIconButton.IconUri = new Uri("/View/images/icon_editprofile.png", UriKind.Relative);
            settingIconButton.Text = AppResources.Settings;
            settingIconButton.Click += settingsButton_Click;

            doneIconButton = new ApplicationBarIconButton();
            doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            doneIconButton.Text = AppResources.OK;
            doneIconButton.Click += doneIconButton_Click;
        }

        private void SetResolutions()
        {
            try
            {
                resolutionGrid.Visibility = Visibility.Visible;
                var res = isPrimaryCam ? AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back) : AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Front);

                if (res != null && res.Count > 0)
                {
                    resolutions = res.Where(r => (r.Height == 1080 || r.Height == 720 || (r.Height == 480 && r.Width == 640) || r.Height == 240)).ToList();

                    if (resolutions != null && resolutions.Count > 0)
                    {
                        try
                        {
                            selectedResolution = resolutions.Where(r => r.Height == 480).First();
                        }
                        catch
                        {
                            selectedResolution = resolutions.First();
                        }

                        resolutionList.ItemsSource = resolutions;
                        resolutionList.SelectedItem = selectedResolution;
                    }
                    else
                    {
                        resolutionGrid.Visibility = Visibility.Collapsed;
                        selectedResolution = res.First();
                    }
                }
                else
                    resolutionGrid.Visibility = Visibility.Collapsed;
            }
            catch
            {
                resolutionGrid.Visibility = Visibility.Collapsed;
                //resolutions are empty
            }
        }

        private void SetCameraDevices()
        {
            var camList = new List<string>();

            var cameraLocations = AudioVideoCaptureDevice.AvailableSensorLocations;

            if (cameraLocations != null && cameraLocations.Count > 0)
            {
                isPrimaryCam = cameraLocations.First() == CameraSensorLocation.Back ? true : false;

                if (cameraLocations.Count > 1)
                {
                    foreach (var cam in cameraLocations)
                        camList.Add(cam.ToString());

                    cameraList.ItemsSource = camList;
                    cameraList.SelectedItem = camList.First();
                }
                else
                    cameraGrid.Visibility = Visibility.Collapsed;
            }
            else
                cameraGrid.Visibility = Visibility.Collapsed;
        }

        async void doneIconButton_Click(object sender, EventArgs e)
        {
            await UpdateRecordingSettings();
        }

        private async System.Threading.Tasks.Task UpdateRecordingSettings()
        {
            videoCaptureDevice.Dispose();
            videoCaptureDevice = isPrimaryCam ? await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Back, selectedResolution) : await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Front, selectedResolution);

            SetUIFromResolution();

            videoRecorderBrush.SetSource(videoCaptureDevice);

            SettingsGrid.Visibility = Visibility.Collapsed;

            if (runningSeconds <= 0 && String.IsNullOrEmpty(txtSize.Text))
                UpdateUI(ButtonState.Initialized);
            else
            {
                addOrRemoveAppBarButton(sendIconButton, true);
                previewGrid.Visibility = Visibility.Visible;
                UpdateUI(ButtonState.Ready);
            }
        }

        private void SetUIFromResolution()
        {
            viewfinderRectangle.Height = selectedResolution.Height > 480 ? 800 : 640;

            VideoPlayer.Height = isPrimaryCam ? 1600 : 960;
            VideoPlayer.Width = isPrimaryCam ? 960 : 640;
            VideoPlayer.Margin = isPrimaryCam ? new Thickness(-1000, -960, 0, 0) : new Thickness(-1440, -1480, 0, 0);
            viewfinderTransform.ScaleX = isPrimaryCam ? 1 : -1;
            playerTransform.ScaleX = isPrimaryCam ? 1 : -1;
        }

        void settingsButton_Click(object sender, EventArgs e)
        {
            UpdateUI(ButtonState.SettingMenu);
            previewGrid.Visibility = Visibility.Collapsed;
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
                if (!ApplicationBar.Buttons.Contains(iconButton) && ApplicationBar.Buttons.Count < 4)
                    ApplicationBar.Buttons.Add(iconButton);
            }
            else
            {
                if (ApplicationBar.Buttons.Contains(iconButton))
                    ApplicationBar.Buttons.Remove(iconButton);
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

            if (runningSeconds > 0 && currentAppState == ButtonState.Recording && !stopIconButton.IsEnabled)
                stopIconButton.IsEnabled = true;

            if (runningSeconds == maxPlayingTime && currentAppState == ButtonState.Recording)
                StopVideoRecording();
        }

        string ConvertToStorageSizeString(long size)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int i = 0;
            double dValue = (double)size;

            while (Math.Round(dValue / 1024) >= 1)
            {
                Debug.WriteLine("Size: " + dValue);

                dValue /= 1024;
                i++;
            }

            return string.Format("{0,2:n1} {1}", dValue, suffixes[i]);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Visible)
            {
                UpdateRecordingSettings();
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New || App.IS_TOMBSTONED)
            {
                SetCameraDevices();
                SetResolutions();
                SetUIFromResolution();
            }

            if(videoCaptureDevice==null)
                await InitializeVideoRecorder();

            if (App.IS_TOMBSTONED)
            {
                object obj;

                if (State.TryGetValue(HikeConstants.VIDEO_SIZE, out obj))
                    txtSize.Text = (string)obj;

                if (State.TryGetValue(HikeConstants.IS_PRIMARY_CAM, out obj))
                    isPrimaryCam = (bool)obj;
                
                if (State.TryGetValue(HikeConstants.VIDEO_RESOLUTION, out obj))
                    selectedResolution = (Windows.Foundation.Size)obj;
                
                if (State.TryGetValue(HikeConstants.VIDEO_THUMBNAIL, out obj))
                    thumbnail = (byte[])obj;
                
                resolutionList.SelectedItem = selectedResolution;

                if (thumbnail != null)
                {
                    if (State.TryGetValue(HikeConstants.VIDEO_FRAME_BYTES, out obj))
                        _snapshotByte = (byte[])obj;
                    
                    var ms = new MemoryStream(_snapshotByte);
                    var bmi = new BitmapImage();
                    bmi.SetSource(ms);
                    snapshotThumbnail.Source = bmi;

                    if (State.TryGetValue(HikeConstants.MAX_VIDEO_PLAYING_TIME, out obj))
                        maxPlayingTime = (Int32)obj;

                    addOrRemoveAppBarButton(sendIconButton, true);
                    UpdateUI(ButtonState.Ready);
                    StartVideoPreview();
                } 

                SetUIFromResolution();
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            progressTimer.Stop();
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (currentAppState == ButtonState.Recording)
                await StopVideoRecording();

            State[HikeConstants.VIDEO_SIZE] = txtSize.Text;
            State[HikeConstants.VIDEO_THUMBNAIL] = thumbnail;
            State[HikeConstants.MAX_VIDEO_PLAYING_TIME] = maxPlayingTime;
            State[HikeConstants.IS_PRIMARY_CAM] = isPrimaryCam;
            State[HikeConstants.VIDEO_RESOLUTION] = selectedResolution;
            State[HikeConstants.VIDEO_FRAME_BYTES] = _snapshotByte;

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
                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);

                        break;

                    // First launch of the application, so no video is available.
                    case ButtonState.Initialized:
                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, true);
                        addOrRemoveAppBarButton(settingIconButton, true);

                        break;

                    // Ready to record, so video is available for viewing.
                    case ButtonState.Ready:
                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, true);
                        addOrRemoveAppBarButton(settingIconButton, true);

                        break;

                    // Video recording is in progress.
                    case ButtonState.Recording:
                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);
                        stopIconButton.IsEnabled = false;

                        break;

                    // Video playback is in progress.
                    case ButtonState.Playback:
                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, true);
                        addOrRemoveAppBarButton(stopIconButton, true);

                        break;

                    // Video playback has been paused.
                    case ButtonState.Paused:
                        addOrRemoveAppBarButton(doneIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, true);

                        break;

                    case ButtonState.SettingMenu:
                        addOrRemoveAppBarButton(sendIconButton, false);
                        addOrRemoveAppBarButton(recordIconButton, false);
                        addOrRemoveAppBarButton(settingIconButton, false);
                        addOrRemoveAppBarButton(stopIconButton, false);
                        addOrRemoveAppBarButton(pauseIconButton, false);
                        addOrRemoveAppBarButton(doneIconButton, true);

                        break;
                    default:
                        break;
                }
                
                currentAppState = currentButtonState;
            });
        }

        private byte[] thumbnail = null;

        public async Task InitializeVideoRecorder()
        {
            try
            {
                if (videoCaptureDevice == null)
                {
                    videoCaptureDevice = isPrimaryCam ? await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Back, selectedResolution) : await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Front, selectedResolution);

                    if (videoCaptureDevice != null)
                    {
                        videoRecorderBrush.SetSource(videoCaptureDevice);

                        UpdateUI(ButtonState.Initialized);
                    }
                    else
                        UpdateUI(ButtonState.CameraNotSupported);
                }
            }
            catch
            {
                UpdateUI(ButtonState.CameraNotSupported);
            }
        }

        void videoCaptureDevice_PreviewFrameAvailable(ICameraCaptureDevice sender, object args)
        {
            if (runningSeconds >= 0 && thumbnail == null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (runningSeconds > 0 && thumbnail == null)
                        {
                            var frameWidth = (int)this.videoCaptureDevice.PreviewResolution.Width;
                            var frameHeight = (int)this.videoCaptureDevice.PreviewResolution.Height;

                            var argbArray = new int[frameWidth * frameHeight];
                            this.videoCaptureDevice.GetPreviewBufferArgb(argbArray);

                            var wb = new WriteableBitmap(frameWidth, frameHeight);
                            argbArray.CopyTo(wb.Pixels, 0);

                            wb = isPrimaryCam ? wb.Rotate(90) : wb.Rotate(270);
                            wb.Invalidate();
                            snapshotThumbnail.Source = wb;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 100);
                                _snapshotByte = ms.ToArray();
                            }

                            int imageWidth, imageHeight;
                            utils.Utils.AdjustAspectRatio(frameHeight,frameWidth, true, out imageWidth, out imageHeight);

                            MemoryStream stream = new MemoryStream();
                            wb.SaveJpeg(stream, imageWidth, imageHeight, 0, 60);
                            thumbnail = stream.ToArray();

                            videoCaptureDevice.PreviewFrameAvailable -= videoCaptureDevice_PreviewFrameAvailable;
                        }
                    });
            }
        }

        private async void StartVideoRecording()
        {
            try
            {
                thumbnail = null;
                videoCaptureDevice.PreviewFrameAvailable += videoCaptureDevice_PreviewFrameAvailable;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile storageFile = await localFolder.CreateFileAsync(isoVideoFileName, CreationCollisionOption.ReplaceExisting);
                path = storageFile.Path;
                if (storageFile != null)
                {
                    videoStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);

                    if (isPrimaryCam)
                        await videoCaptureDevice.FocusAsync();

                    await videoCaptureDevice.StartRecordingToStreamAsync(videoStream);
                }

                UpdateUI(ButtonState.Recording);
                txtDebug.Opacity = 1;
                runningSeconds = -1;
                maxPlayingTime = maxVideoRecordTime;
                progressTimer.Start();
                updateProgress();

                videoRecorderBrush.SetSource(videoCaptureDevice);
            }
            catch (Exception e)
            {
                Debug.WriteLine("RecoedVideo.xaml :: StartVideoRecording, Exception : " + e.StackTrace);
            }
        }

        // Set the recording state: stop recording.
        private async Task StopVideoRecording()
        {
            try
            {
                videoCaptureDevice.PreviewFrameAvailable -= videoCaptureDevice_PreviewFrameAvailable;
                
                if (currentAppState == ButtonState.Recording)
                {
                    await videoCaptureDevice.StopRecordingAsync();
                    videoStream.AsStream().Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("RecoedVideo.xaml :: StopVideoRecording, Exception : " + e.StackTrace);
            }

            addOrRemoveAppBarButton(sendIconButton, true);
            txtDebug.Opacity = 0;
            maxPlayingTime = runningSeconds;
            runningSeconds = -1;
            progressTimer.Stop();
            updateProgress();

            try
            {
                Byte[] fileBytes = null;
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.TEMP_VIDEO_NAME, out fileBytes);

                txtSize.Text = ConvertToStorageSizeString(fileBytes.Length);
            }
            catch
            {
                txtSize.Text = String.Empty;
            }

            StartVideoPreview();
        }

        private async void StartVideoPreview()
        {
            try
            {
                previewGrid.Visibility = Visibility.Visible;
                viewfinderRectangle.Visibility = Visibility.Collapsed;

                if (videoCaptureDevice == null)
                    await InitializeVideoRecorder();

                videoRecorderBrush.SetSource(videoCaptureDevice);
                viewfinderRectangle.Fill = videoRecorderBrush;

                UpdateUI(ButtonState.Ready);
            }
            catch (Exception e)
            {
            }
        }

        private void StartRecording_Click(object sender, EventArgs e)
        {
            try
            {
                viewfinderRectangle.Visibility = Visibility.Visible;
                previewGrid.Visibility = Visibility.Collapsed;
                txtSize.Text = String.Empty;

                addOrRemoveAppBarButton(recordIconButton, false);
                addOrRemoveAppBarButton(settingIconButton, false);
                addOrRemoveAppBarButton(sendIconButton, false);
                StartVideoRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Record Video :: StartRecording_Click , Exception:" + ex.StackTrace);
            }
        }

        private void StopPlaybackRecording_Click(object sender, EventArgs e)
        {
            addOrRemoveAppBarButton(stopIconButton, false);

            txtDebug.Opacity = 0;
            progressTimer.Stop();
            updateProgress();

            if (currentAppState == ButtonState.Recording)
                StopVideoRecording();
            else
            {
                runningSeconds = -1;
                DisposeVideoPlayer();
                StartVideoPreview();
            }
        }

        private void StartPlayback_Click(object sender, EventArgs e)
        {
            PlayVideo();
        }

        private void PlayVideo()
        {
            txtDebug.Opacity = 1;
            previewGrid.Visibility = Visibility.Collapsed;
            VideoPlayer.Visibility = Visibility.Visible;

            if (isoVideoFile != null && VideoPlayer.Source != null)
                VideoPlayer.Play();
            else
            {
                viewfinderRectangle.Fill = null;
                isoVideoFile = new IsolatedStorageFileStream(isoVideoFileName, FileMode.Open, FileAccess.Read, IsolatedStorageFile.GetUserStoreForApplication());
                VideoPlayer.SetSource(isoVideoFile);
                VideoPlayer.MediaEnded += new RoutedEventHandler(VideoPlayerMediaEnded);
                VideoPlayer.Play();
            }

            progressTimer.Start();

            UpdateUI(ButtonState.Playback);
        }

        private void PausePlayback_Click(object sender, EventArgs e)
        {
            progressTimer.Stop();
            addOrRemoveAppBarButton(pauseIconButton, false);
            
            if (VideoPlayer != null)
                VideoPlayer.Pause();
        
            UpdateUI(ButtonState.Paused);
        }

        private void DisposeVideoPlayer()
        {
            if (VideoPlayer != null)
            {
                VideoPlayer.Visibility = Visibility.Collapsed;
                VideoPlayer.Stop();
                VideoPlayer.Source = null;
                VideoPlayer.MediaEnded -= VideoPlayerMediaEnded;
            }
        }

        private void DisposeVideoRecorder()
        {
            if (videoCaptureDevice != null)
                videoCaptureDevice.Dispose();

            videoCaptureDevice = null;
        }

        public async void VideoPlayerMediaEnded(object sender, RoutedEventArgs e)
        {
            DisposeVideoPlayer();
            await StopVideoRecording();
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

        private void StartPlayback_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PlayVideo();
        }
    }
}
