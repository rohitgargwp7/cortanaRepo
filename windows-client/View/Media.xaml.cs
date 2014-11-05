using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class Media : PhoneApplicationPage
    {
        public Media()
        {
            InitializeComponent();
            int selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_IMAGE, out selIndex))
                selIndex = 2;

            autoDownloadImageListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_AUDIO, out selIndex))
                selIndex = 1;

            autoDownloadAudioListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_VIDEO, out selIndex))
                selIndex = 1;

            autoDownloadVideoListPicker.SelectedIndex = selIndex;
        }

        private void ListPicker_Loaded(object sender, RoutedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            listPicker.SelectionChanged -= autoDownloadListPicker_SelectionChanged;
            listPicker.SelectionChanged += autoDownloadListPicker_SelectionChanged;
        }

        private void autoDownloadListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            App.WriteToIsoStorageSettings(listPicker.Tag.ToString(), listPicker.SelectedIndex);
        }
    }
}