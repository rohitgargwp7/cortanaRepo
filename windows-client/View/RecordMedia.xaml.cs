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

        private int maxPlayDuration = 120;
        private int state = 0;


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
            // Get audio data in 1/2 second chunks
            microphone.BufferDuration = TimeSpan.FromMilliseconds(500);
            // Allocate memory to hold the audio data
            buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
            // Set the stream back to zero in case there is already something in it
            stream.SetLength(0);
            // Start recording
            microphone.Start();
            timeBar.Opacity = 1;
            progressTimer.Start();
            statusImage.Source = new BitmapImage(new Uri("/images/icon_record.png", UriKind.Relative));
            message.Text = "RECORDING";
        }

        void showProgress(object sender, EventArgs e)
        {
            runningSeconds++;
            runningTime.Text = formatTime(runningSeconds);
            if (runningSeconds >= 120)
                stop();
        }

        private void stop()
        {
            if (microphone.State == MicrophoneState.Started)
            {
                // In RECORD mode, user clicked the 
                // stop button to end recording
                microphone.Stop();
            }
            else if (soundInstance.State == SoundState.Playing)
            {
                soundInstance.Stop();
            }
            timeBar.Opacity = 0;
            maxPlayDuration = runningSeconds;
            runningSeconds = 0;
            message.Text = "TAP TO PLAY";
            statusImage.Source = new BitmapImage(new Uri("/images/icon_play.png", UriKind.Relative));
            progressTimer.Stop();
        }

        private void play()
        {
            if (stream.Length > 0)
            {
                // Play the audio in a new thread so the UI can update.
                Thread soundThread = new Thread(new ThreadStart(playSound));
                soundThread.Start();
            }
            timeBar.Opacity = 1;
            runningSeconds = 0;
            progressTimer.Start();
            message.Text = "PLAYING";
            statusImage.Source = new BitmapImage(new Uri("/images/icon_play.png", UriKind.Relative));

            maxPlayingTime.Text = " / " + formatTime(maxPlayDuration);
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
            switch (state)
            {
                case 0:
                    record();
                    state = 1;
                    break;
                case 1:
                    stop();
                    state = 2;
                    break;
                case 2:
                    play();
                    break;
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            state = 0;
            maxPlayDuration = 120;
            message.Text = "TAP ICON TO RECORD";
            statusImage.Source = new BitmapImage(new Uri("/images/icon_record.png", UriKind.Relative));
        }

        private void send_Click(object sender, EventArgs e)
        {

        }

    }
}