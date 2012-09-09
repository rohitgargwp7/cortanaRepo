using System;
using System.Windows;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class PlayMedia : PhoneApplicationPage
    {
        public PlayMedia()
        {
            InitializeComponent();

            if (PhoneApplicationService.Current.State.ContainsKey("objectForFileTransfer"))
            {
                object[] fileTapped = (object[])PhoneApplicationService.Current.State["objectForFileTransfer"];
                long messsageId = (long)fileTapped[0];
                string msisdn = (string)fileTapped[1];
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messsageId);

                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (myIsolatedStorage.FileExists(filePath))
                    {
                        using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                        {
                            FileMedia.SetSource(fileStream);
                            FileMedia.Play();
                        }
                    }
                }
            }
        }


        private void Play_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.FileMedia.Play();
        }

        private void Pause_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.FileMedia.Pause();
        }

        private void Stop_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.FileMedia.Stop();
        }

        private void FileMedia_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
        }

        private void FileMedia_MediaEnded(object sender, RoutedEventArgs e)
        {
            FileMedia.Stop();
        }

        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FileMedia.Volume = (double)this.volumeSlider.Value;
        }

        private void SeekToMediaPosition(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int SliderValue = (int)timelineSlider.Value;

            // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds. 
            // Create a TimeSpan with miliseconds equal to the slider value.
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            FileMedia.Position = ts;
        }

        private void FileMedia_MediaOpened(object sender, RoutedEventArgs e)
        {
            timelineSlider.Maximum = FileMedia.NaturalDuration.TimeSpan.TotalMilliseconds;
        }
    }
}