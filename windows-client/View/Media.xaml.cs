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
using CommonLibrary.Languages;

namespace windows_client.View
{
    public partial class Media : PhoneApplicationPage
    {
        public Media()
        {
            InitializeComponent();
            
            int selIndex;
            
            if (!HikeInstantiation.AppSettings.TryGetValue(FTBasedConstants.AUTO_DOWNLOAD_IMAGE, out selIndex))
                selIndex = 2;

            autoDownloadImageListPicker.SelectedIndex = selIndex;

            if (!HikeInstantiation.AppSettings.TryGetValue(FTBasedConstants.AUTO_DOWNLOAD_AUDIO, out selIndex))
                selIndex = 1;

            autoDownloadAudioListPicker.SelectedIndex = selIndex;

            if (!HikeInstantiation.AppSettings.TryGetValue(FTBasedConstants.AUTO_DOWNLOAD_VIDEO, out selIndex))
                selIndex = 1;

            autoDownloadVideoListPicker.SelectedIndex = selIndex;

            bool value = !HikeInstantiation.AppSettings.Contains(AppSettingsKeys.AUTO_RESUME_SETTING);
            autoResumeToggle.IsChecked = value;
            autoResumeToggle.Content = value ? AppResources.On : AppResources.Off;
        }

        private void Toggle_Loaded(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
                return;

            toggleSwitch.Checked   -= Toggle_Checked;
            toggleSwitch.Checked   += Toggle_Checked;
            toggleSwitch.Unchecked -= Toggle_Unchecked;
            toggleSwitch.Unchecked += Toggle_Unchecked;
        }

        private void Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
                return;

            toggleSwitch.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(toggleSwitch.Tag.ToString(),false);
        }

        private void Toggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
                return;

            toggleSwitch.Content = AppResources.On;
            HikeInstantiation.RemoveKeyFromAppSettings(toggleSwitch.Tag.ToString());
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

            HikeInstantiation.WriteToIsoStorageSettings(listPicker.Tag.ToString(), listPicker.SelectedIndex);
        }
    }
}