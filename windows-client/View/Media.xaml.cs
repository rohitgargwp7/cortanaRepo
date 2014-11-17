using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class Media : PhoneApplicationPage
    {
        public Media()
        {
            InitializeComponent();
            int selIndex;
            
            //TODO::MOHIT 
            //autoDownloadImageListPicker.Tag = HikeConstants.AUTO_DOWNLOAD_IMAGE;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_IMAGE, out selIndex))
                selIndex = 2;

            autoDownloadImageListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_AUDIO, out selIndex))
                selIndex = 1;

            autoDownloadAudioListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_VIDEO, out selIndex))
                selIndex = 1;

            autoDownloadVideoListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.SET_IMAGE_QUALITY, out selIndex))
                selIndex = 3;

            setImageQualityListPicker.SelectedIndex = selIndex;



            bool value = !App.appSettings.Contains(App.AUTO_RESUME_SETTING);
            autoResumeToggle.IsChecked = value;
            autoResumeToggle.Content = value ? AppResources.On : AppResources.Off;
        }

        private void ListPicker_Loaded(object sender, RoutedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            listPicker.SelectionChanged -= autoDownloadListPicker_SelectionChanged;
            listPicker.SelectionChanged += autoDownloadListPicker_SelectionChanged;
        }

        private void ImageQuality_ListPicker_Loaded(object sender, RoutedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            listPicker.SelectionChanged -= ImageQualityListPicker_SelectionChanged;
            listPicker.SelectionChanged += ImageQualityListPicker_SelectionChanged;
        }

        private void autoDownloadListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;
            if(listPicker.SelectedIndex != 3)
                App.WriteToIsoStorageSettings(listPicker.Tag.ToString(), listPicker.SelectedIndex);
        }

        private void ImageQualityListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;
            if (listPicker == null)
                return;
            if (listPicker.SelectedIndex == 3)
                App.RemoveKeyFromAppSettings(HikeConstants.SET_IMAGE_QUALITY);
            else
                App.WriteToIsoStorageSettings(listPicker.Tag.ToString(), listPicker.SelectedIndex);
        }

        private void Toggle_Loaded(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
                return;

            toggleSwitch.Checked -= Toggle_Checked;
            toggleSwitch.Checked += Toggle_Checked;
            toggleSwitch.Unchecked -= Toggle_Unchecked;
            toggleSwitch.Unchecked += Toggle_Unchecked;
        }

        private void Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
                return;

            toggleSwitch.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(toggleSwitch.Tag.ToString(), false);
        }

        private void Toggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
                return;

            toggleSwitch.Content = AppResources.On;
            App.RemoveKeyFromAppSettings(toggleSwitch.Tag.ToString());
            
            if (toggleSwitch.Tag.ToString() == App.AUTO_RESUME_SETTING)
                FileTransfers.FileTransferManager.Instance.PopulatePreviousTasks();
        }
    }
}