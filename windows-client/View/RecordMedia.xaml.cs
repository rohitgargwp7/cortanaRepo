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

                    // Update the UI to reflect that the 
                    // sound has stopped playing
                    SetButtonStates(true, true, false);
                    UserHelp.Text = "press play\nor record";
                    StatusImage.Source = blankImage;
                }
            }
        }

        /// <summary>
        /// The Microphone.BufferReady event handler.
        /// Gets the audio data from the microphone and stores it in a buffer,
        /// then writes that buffer to a stream for later playback.
        /// Any action in this event handler should be quick!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void microphone_BufferReady(object sender, EventArgs e)
        {
            // Retrieve audio data
            microphone.GetData(buffer);

            // Store the audio data in a stream
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Handles the Click event for the record button.
        /// Sets up the microphone and data buffers to collect audio data,
        /// then starts the microphone. Also, updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recordButton_Click(object sender, EventArgs e)
        {
            // Get audio data in 1/2 second chunks
            microphone.BufferDuration = TimeSpan.FromMilliseconds(500);

            // Allocate memory to hold the audio data
            buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];

            // Set the stream back to zero in case there is already something in it
            stream.SetLength(0);

            // Start recording
            microphone.Start();

            SetButtonStates(false, false, true);
            UserHelp.Text = "record";
            StatusImage.Source = microphoneImage;

            progressTimer.Start();
            message.Text = "RECORDGING";


        }
        void showProgress(object sender, EventArgs e)
        {
            runningSeconds++;
            runningTime.Text = Convert.ToString(runningSeconds);
        }
        /// <summary>
        /// Handles the Click event for the stop button.
        /// Stops the microphone from collecting audio and updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopButton_Click(object sender, EventArgs e)
        {
            if (microphone.State == MicrophoneState.Started)
            {
                // In RECORD mode, user clicked the 
                // stop button to end recording
                microphone.Stop();
            }
            else if (soundInstance.State == SoundState.Playing)
            {
                // In PLAY mode, user clicked the 
                // stop button to end playing back
                soundInstance.Stop();
            }

            SetButtonStates(true, true, false);
            UserHelp.Text = "ready";
            StatusImage.Source = blankImage;

            runningSeconds = 0;
            message.Text = "TAP TO PLAY";
            progressTimer.Stop();
        }

        /// <summary>
        /// Handles the Click event for the play button.
        /// Plays the audio collected from the microphone and updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playButton_Click(object sender, EventArgs e)
        {
            if (stream.Length > 0)
            {
                // Update the UI to reflect that
                // sound is playing
                SetButtonStates(false, false, true);
                UserHelp.Text = "play";
                StatusImage.Source = speakerImage;

                // Play the audio in a new thread so the UI can update.
                Thread soundThread = new Thread(new ThreadStart(playSound));
                soundThread.Start();
                
            }
            runningSeconds = 0;
            progressTimer.Start();
            message.Text = "PLAYING";
        }

        /// <summary>
        /// Plays the audio using SoundEffectInstance 
        /// so we can monitor the playback status.
        /// </summary>
        private void playSound()
        {
            // Play audio using SoundEffectInstance so we can monitor it's State 
            // and update the UI in the dt_Tick handler when it is done playing.
            SoundEffect sound = new SoundEffect(stream.ToArray(), microphone.SampleRate, AudioChannels.Mono);
            soundInstance = sound.CreateInstance();
            soundIsPlaying = true;
            soundInstance.Play();
        }

        /// <summary>
        /// Helper method to change the IsEnabled property for the ApplicationBarIconButtons.
        /// </summary>
        /// <param name="recordEnabled">New state for the record button.</param>
        /// <param name="playEnabled">New state for the play button.</param>
        /// <param name="stopEnabled">New state for the stop button.</param>
        private void SetButtonStates(bool recordEnabled, bool playEnabled, bool stopEnabled)
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = recordEnabled;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = playEnabled;
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = stopEnabled;
        }

    }
}