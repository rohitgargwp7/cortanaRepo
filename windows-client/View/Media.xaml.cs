using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using CommonLibrary.Misc;
using CommonLibrary.Constants;

namespace windows_client.View
{
    public partial class Media : PhoneApplicationPage
    {
        public Media()
        {
            InitializeComponent();
            
            int selIndex;
            
            if (!HikeInstantiation.AppSettings.TryGetValue(FTBasedConstants.AUTO_DOWNLOAD_IMAGE, out selIndex))
                selIndex = 0;

            autoDownloadImageListPicker.SelectedIndex = selIndex;

            if (!HikeInstantiation.AppSettings.TryGetValue(FTBasedConstants.AUTO_DOWNLOAD_AUDIO, out selIndex))
                selIndex = 0;

            autoDownloadAudioListPicker.SelectedIndex = selIndex;

            if (!HikeInstantiation.AppSettings.TryGetValue(FTBasedConstants.AUTO_DOWNLOAD_VIDEO, out selIndex))
                selIndex = 0;

            autoDownloadVideoListPicker.SelectedIndex = selIndex;

            autoDownloadImageListPicker.SelectionChanged -= autoDownloadListPicker_SelectionChanged;
            autoDownloadAudioListPicker.SelectionChanged -= autoDownloadListPicker_SelectionChanged;
            autoDownloadVideoListPicker.SelectionChanged -= autoDownloadListPicker_SelectionChanged;
            
            autoDownloadImageListPicker.SelectionChanged += autoDownloadListPicker_SelectionChanged;
            autoDownloadAudioListPicker.SelectionChanged += autoDownloadListPicker_SelectionChanged;
            autoDownloadVideoListPicker.SelectionChanged += autoDownloadListPicker_SelectionChanged;
        }

        private void autoDownloadListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            HikeInstantiation.WriteToIsoStorageSettings(listPicker.Tag.ToString(), listPicker.SelectedIndex);
        }
    }
}