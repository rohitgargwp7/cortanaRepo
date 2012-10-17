using System;
using Microsoft.Phone.Controls;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;


namespace windows_client.View
{
    public partial class RecordMedia : PhoneApplicationPage
    {
        private Microphone microphone = Microphone.Default;     // Object representing the physical microphone on the device
        private byte[] buffer;                                  // Dynamic buffer to retrieve audio data from the microphone
        private MemoryStream stream = new MemoryStream();       // Stores the audio data for later playback
        private SoundEffectInstance soundInstance;              // Used to play back audio
        private bool soundIsPlaying = false;                    // Flag to monitor the state of sound playback

        private DispatcherTimer progressTimer;
        private int runningSeconds = 0;

        // Status images
        private BitmapImage blankImage;
        private BitmapImage microphoneImage;
        private BitmapImage speakerImage;
        private byte[] _buffer;
        private TimeSpan _duration;

        private BitmapImage recordIcon = new BitmapImage(new Uri("/View/images/icon_record.png", UriKind.Relative));
        private BitmapImage playIcon = new BitmapImage(new Uri("/View/images/icon_play.png", UriKind.Relative));
        private BitmapImage stopIcon = new BitmapImage(new Uri("/View/images/icon_play.png", UriKind.Relative));
        private BitmapImage playStopIcon = new BitmapImage(new Uri("/View/images/icon_play_stop.png", UriKind.Relative));

        private ApplicationBar appBar;
        ApplicationBarIconButton cancelIconButton = null;
        ApplicationBarIconButton sendIconButton = null;
        private int recordedDuration = -1;


        private enum RecorderState
        { 
            NOTHING_RECORDED = 0,
            RECORDED,
            RECORDING,
            PLAYING,
        }

        private RecorderState myState = RecorderState.NOTHING_RECORDED;

        public RecordMedia()
        {
            InitializeComponent();

            // Timer to simulate the XNA Framework game loop (Microphone is 
            // from the XNA Framework). We also use this timer to monitor the 
            // state of audio playback so we can update the UI appropriately.
            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromMilliseconds(33);
            dt.Tick += new EventHandler(dt_Tick);
            dt.Start();

            _duration = microphone.BufferDuration;
            _buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];

            // Event handler for getting audio data when the buffer is full
            microphone.BufferReady += new EventHandler<EventArgs>(microphone_BufferReady);
            
            blankImage = new BitmapImage(new Uri("Images/blank.png", UriKind.RelativeOrAbsolute));
            microphoneImage = new BitmapImage(new Uri("images/microphone.png", UriKind.RelativeOrAbsolute));
            speakerImage = new BitmapImage(new Uri("images/speaker.png", UriKind.RelativeOrAbsolute));

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += new EventHandler(showProgress);

            //app bar
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            //add icon for cancel
            cancelIconButton = new ApplicationBarIconButton();
            cancelIconButton.IconUri = new Uri("/View/images/icon_refresh.png", UriKind.Relative);
            cancelIconButton.Text = "re-record";
            cancelIconButton.Click += new EventHandler(refresh_Click);
            cancelIconButton.IsEnabled = false;
            appBar.Buttons.Add(cancelIconButton);

            //add icon for send
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            sendIconButton.Text = "send";
            sendIconButton.Click += new EventHandler(send_Click);
            sendIconButton.IsEnabled = false;
            appBar.Buttons.Add(sendIconButton);

            recordMedia.ApplicationBar = appBar;
        }

        void dt_Tick(object sender, EventArgs e)
        {
            try { FrameworkDispatcher.Update(); }
            catch { }

            if (true == soundIsPlaying)
            {
                if (soundInstance.State != SoundState.Playing)
                {
                    // Audio has finished playing
                    soundIsPlaying = false;
                    stop();
                }
            }
        }

        void microphone_BufferReady(object sender, EventArgs e)
        {
            // Retrieve audio data
            microphone.GetData(buffer);
            stream.Write(buffer, 0, buffer.Length);
        }

        private void record()
        {
            progressTimer.Start();

            // Get audio data in 1/2 second chunks
            microphone.BufferDuration = TimeSpan.FromMilliseconds(300);
            // Allocate memory to hold the audio data
            buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
            // Set the stream back to zero in case there is already something in it
            stream.SetLength(0);

            WriteWavHeader(stream, microphone.SampleRate);

            // Start recording
            microphone.Start();
            timeBar.Opacity = 1;
            statusImage.Source = recordIcon;
            message.Text = "RECORDING";
            maxPlayingTime.Text = " / " + formatTime(HikeConstants.MAX_AUDIO_RECORDTIME_SUPPORTED);
            cancelIconButton.IsEnabled = true;
            sendIconButton.IsEnabled = false;
            myState = RecorderState.RECORDING;
        }

        void showProgress(object sender, EventArgs e)
        {
            runningTime.Text = formatTime(runningSeconds);
            if (runningSeconds >= HikeConstants.MAX_AUDIO_RECORDTIME_SUPPORTED)
                stop();
            runningSeconds++;
        }

        private void stop()
        {
            progressTimer.Stop();
            if (microphone.State == MicrophoneState.Started)
            {
                // In RECORD mode, user clicked the 
                // stop button to end recording
                microphone.Stop();
                UpdateWavHeader(stream);
            }
            else if (soundInstance!=null && soundInstance.State == SoundState.Playing)
            {
                soundInstance.Stop();
            }
            timeBar.Opacity = 0;
            if (myState == RecorderState.RECORDING)
            {
                recordedDuration = runningSeconds;
                runningTime.Text = formatTime(0);
            }
            runningSeconds = 0;
            message.Text = "TAP TO PLAY";
            statusImage.Source = playIcon;
            sendIconButton.IsEnabled = true;
            myState = RecorderState.RECORDED;
        }

        private void play()
        {
            progressTimer.Start();
            if (stream.Length > 0)
            {
                // Play the audio in a new thread so the UI can update.
                Thread soundThread = new Thread(new ThreadStart(playSound));
                soundThread.Start();
            }
            timeBar.Opacity = 1;
            runningSeconds = 0;
            message.Text = "PLAYING";
            statusImage.Source = playStopIcon;
            maxPlayingTime.Text = " / " + formatTime(recordedDuration);
            myState = RecorderState.PLAYING;
        }

        private void playSound()
        {
            // Play audio using SoundEffectInstance so we can monitor it's State 
            // and update the UI in the dt_Tick handler when it is done playing.
            SoundEffect sound = new SoundEffect(stream.ToArray(), microphone.SampleRate, AudioChannels.Mono);
            soundInstance = sound.CreateInstance();
            soundIsPlaying = true;
            soundInstance.Play();
        }

        private string formatTime(int seconds)
        {
            int minute = seconds / 60;
            int secs = seconds % 60;
            return minute.ToString("00") + ":" + secs.ToString("00");
        }

        private void Record_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MicrophoneState m = microphone.State;
            switch (myState)
            {
                case RecorderState.NOTHING_RECORDED:
                    record();
                    break;
                case RecorderState.RECORDED:
                    play();
                    break;
                default:
                    stop();
                    break;
            }
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            if (myState == RecorderState.RECORDING || myState == RecorderState.PLAYING)
                stop();
            message.Text = "TAP ICON TO RECORD";
            statusImage.Source = recordIcon;
            myState = RecorderState.NOTHING_RECORDED;
        }

        private void send_Click(object sender, EventArgs e)
        {
            if (stream != null)
            {
                byte[] audioBytes = stream.ToArray();
                if (audioBytes != null && audioBytes.Length > 0)
                {
                    PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED] = stream.ToArray();
                    NavigationService.GoBack();
                }
            }
        }

        public void WriteWavHeader(Stream stream, int sampleRate)
        {
            const int bitsPerSample = 16;
            const int bytesPerSample = bitsPerSample / 8;
            var encoding = System.Text.Encoding.UTF8;
            // ChunkID Contains the letters "RIFF" in ASCII form (0x52494646 big-endian form).
            stream.Write(encoding.GetBytes("RIFF"), 0, 4);

            // NOTE this will be filled in later
            stream.Write(BitConverter.GetBytes(0), 0, 4);

            // Format Contains the letters "WAVE"(0x57415645 big-endian form).
            stream.Write(encoding.GetBytes("WAVE"), 0, 4);

            // Subchunk1ID Contains the letters "fmt " (0x666d7420 big-endian form).
            stream.Write(encoding.GetBytes("fmt "), 0, 4);

            // Subchunk1Size 16 for PCM.  This is the size of therest of the Subchunk which follows this number.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // AudioFormat PCM = 1 (i.e. Linear quantization) Values other than 1 indicate some form of compression.
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);

            // NumChannels Mono = 1, Stereo = 2, etc.
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);

            // SampleRate 8000, 44100, etc.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // ByteRate =  SampleRate * NumChannels * BitsPerSample/8
            stream.Write(BitConverter.GetBytes(sampleRate * bytesPerSample), 0, 4);

            // BlockAlign NumChannels * BitsPerSample/8 The number of bytes for one sample including all channels.
            stream.Write(BitConverter.GetBytes((short)(bytesPerSample)), 0, 2);

            // BitsPerSample    8 bits = 8, 16 bits = 16, etc.
            stream.Write(BitConverter.GetBytes((short)(bitsPerSample)), 0, 2);

            // Subchunk2ID Contains the letters "data" (0x64617461 big-endian form).
            stream.Write(encoding.GetBytes("data"), 0, 4);

            // NOTE to be filled in later
            stream.Write(BitConverter.GetBytes(0), 0, 4);
        }

        public void UpdateWavHeader(Stream stream)
        {
            if (!stream.CanSeek) throw new Exception("Can't seek stream to update wav header");

            var oldPos = stream.Position;

            // ChunkSize  36 + SubChunk2Size
            stream.Seek(4, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)stream.Length - 8), 0, 4);

            // Subchunk2Size == NumSamples * NumChannels * BitsPerSample/8 This is the number of bytes in the data.
            stream.Seek(40, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)stream.Length - 44), 0, 4);

            stream.Seek(oldPos, SeekOrigin.Begin);
        }

    }
}