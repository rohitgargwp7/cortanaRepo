﻿using System;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using windows_client.utils;
using System.Diagnostics;
namespace windows_client.View
{
    public partial class DisplayImage : PhoneApplicationPage
    {
        private string msisdn;

        public DisplayImage()
        {
            InitializeComponent();
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("objectForFileTransfer");
            PhoneApplicationService.Current.State.Remove("displayProfilePic");
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("objectForFileTransfer"))
            {
                object[] fileTapped = (object[])PhoneApplicationService.Current.State["objectForFileTransfer"];
                long messsageId = (long)fileTapped[0];
                msisdn = (string)fileTapped[1];
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messsageId);
                byte[] filebytes;
                MiscDBUtil.readFileFromIsolatedStorage(filePath, out filebytes);
                this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(filebytes);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey("displayProfilePic"))
            {
                string fileName;
                object[] profilePicTapped = (object[])PhoneApplicationService.Current.State["displayProfilePic"];
                msisdn = (string)profilePicTapped[0];
                string filePath = msisdn + HikeConstants.FULL_VIEW_IMAGE_PREFIX;

                //check if image is already stored
                byte[] fullViewBytes = MiscDBUtil.getThumbNailForMsisdn(filePath);
                if (fullViewBytes != null && fullViewBytes.Length > 0)
                {
                    this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(fullViewBytes);
                }
                else if (MiscDBUtil.hasCustomProfileImage(msisdn))
                {
                    fileName = msisdn + HikeConstants.FULL_VIEW_IMAGE_PREFIX;
                    loadingProgress.Opacity = 1;
                    if (!Utils.isGroupConversation(msisdn))
                    {
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/account/avatar/" + msisdn + "?fullsize=true", getProfilePic_Callback, true, fileName);
                    }
                    else
                    {
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/group/" + msisdn + "/avatar?fullsize=true", getProfilePic_Callback, true, fileName);
                    }
                }
                else
                {
                    fileName = UI_Utils.Instance.getDefaultAvatarFileName(msisdn,
                        Utils.isGroupConversation(msisdn));
                    byte[] defaultImageBytes = MiscDBUtil.getThumbNailForMsisdn(fileName);
                    if (defaultImageBytes == null || defaultImageBytes.Length == 0)
                    {
                        loadingProgress.Opacity = 1;
                        AccountUtils.createGetRequest(AccountUtils.AVATAR_BASE + "/static/avatars/" + fileName, getProfilePic_Callback, false, fileName);
                    }
                    else
                    {
                        this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(defaultImageBytes);
                    }
                }
            }
        }

        public void getProfilePic_Callback(byte[] fullBytes, object fName)
        {
            string fileName = fName as string;
            if (fullBytes != null && fullBytes.Length > 0)
                MiscDBUtil.saveAvatarImage(fileName, fullBytes, false);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                loadingProgress.Opacity = 0;
                if (fullBytes != null && fullBytes.Length > 0)
                {
                    this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(fullBytes);
                }
                else
                {
                    byte[] smallThumbnailImage = MiscDBUtil.getThumbNailForMsisdn(msisdn);
                    if (smallThumbnailImage != null && smallThumbnailImage.Length > 0)
                    {
                        this.FileImage.Source = UI_Utils.Instance.createImageFromBytes(smallThumbnailImage);
                    }
                    else
                    {
                        BitmapImage defaultImage = null;
                        if (Utils.isGroupConversation(msisdn))
                        {
                            defaultImage = UI_Utils.Instance.getDefaultGroupAvatar(msisdn);
                        }
                        else
                        {
                            defaultImage = UI_Utils.Instance.getDefaultAvatar(msisdn);
                        }
                        this.FileImage.Source = defaultImage;
                    }
                }
            });
        }
    }
}