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
using windows_client.FileTransfers;

namespace windows_client.View
{
    public partial class Media : PhoneApplicationPage
    {
        public Media()
        {
            InitializeComponent();
            int selIndex;
            ImageQuality imgQlty;
            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_IMAGE, out selIndex))
                selIndex = 2;

            autoDownloadImageListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_AUDIO, out selIndex))
                selIndex = 1;

            autoDownloadAudioListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_VIDEO, out selIndex))
                selIndex = 1;

            autoDownloadVideoListPicker.SelectedIndex = selIndex;

            if (!App.appSettings.TryGetValue(HikeConstants.SET_IMAGE_QUALITY, out imgQlty))
                selIndex = 3;
            else
                selIndex = (int)imgQlty;

            setImageQualityListPicker.SelectedIndex = selIndex;

            bool value = !App.appSettings.Contains(App.AUTO_RESUME_SETTING);
            autoResumeToggle.IsChecked = value;
            autoResumeToggle.Content = value ? AppResources.On : AppResources.Off;
        }

        private void setImageQuality_ListPicker_Loaded(object sender, RoutedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            listPicker.SelectionChanged -= setImageQuality_ListPicker_SelectionChanged;
            listPicker.SelectionChanged += setImageQuality_ListPicker_SelectionChanged;        
        }

        private void setImageQuality_ListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            //Index 3 corresponds to "Ask Every Time" option
            if (listPicker.SelectedIndex == 3)
                App.RemoveKeyFromAppSettings(HikeConstants.SET_IMAGE_QUALITY);                
            else
                App.WriteToIsoStorageSettings(listPicker.Tag.ToString(), (ImageQuality)(byte)listPicker.SelectedIndex);                
        }     

        private void MediaSettings_ListPicker_Loaded(object sender, RoutedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            listPicker.SelectionChanged -= MediaSettings_ListPicker_SelectionChanged;
            listPicker.SelectionChanged += MediaSettings_ListPicker_SelectionChanged;
        }

        private void MediaSettings_ListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPicker listPicker = sender as ListPicker;

            if (listPicker == null)
                return;

            if(listPicker.SelectedIndex != 3)
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