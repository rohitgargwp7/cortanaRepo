using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using WindowsPhoneRuntimeComponent1;
using windows_client.Model.Video;
using System.Diagnostics;
using System.Windows.Resources;
using windows_client.utils;

namespace windows_client.View
{
    public partial class ViewVideos : PhoneApplicationPage
    {
        public ViewVideos()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ushort totalVideos = WindowsPhoneRuntimeComponent.GetVideoCount();

            List<VideoType> listVideos = new List<VideoType>();
            for (int i = 0; i < totalVideos; i++)
            {
                Byte[] thumbBytes = new byte[30000];
                string filePath = string.Empty;
                string fileName = string.Empty;
                WindowsPhoneRuntimeComponent.myfunc((byte)i, thumbBytes, out filePath, out fileName);
                VideoType video = new VideoType(fileName, filePath, thumbBytes);
                listVideos.Add(video);
            }

            llsVideos.ItemsSource = listVideos;

        }

        private void llsVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoType selectedVideo = llsVideos.SelectedItem as VideoType;
            if (selectedVideo == null)
                return;
            llsVideos.SelectedItem = null;

            StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri(selectedVideo.FilePath, UriKind.Relative));
            byte[] videoBytes = AccountUtils.StreamToByteArray(streamInfo.Stream);
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_THUMB_SHARED] = selectedVideo.Thumbnail;
            NavigationService.GoBack();

        }
    }
}