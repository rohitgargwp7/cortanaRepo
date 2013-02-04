﻿using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using windows_client.Controls.StatusUpdate;
using windows_client.DbUtils;
using windows_client.Model;

namespace windows_client.utils
{
    class StatusUpdateHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile StatusUpdateHelper instance = null;

        public static StatusUpdateHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new StatusUpdateHelper();
                        }
                    }
                }
                return instance;
            }
        }

        public StatusUpdateBox createStatusUIObject(BitmapImage img,StatusMessage status, EventHandler<GestureEventArgs> statusBoxTap)
        {
            string userName;          
            if (App.ViewModel.ConvMap.ContainsKey(status.Msisdn))
            {
                userName = App.ViewModel.ConvMap[status.Msisdn].NameToShow;
            }
            else if (App.MSISDN == status.Msisdn)
            {
                if (!App.appSettings.TryGetValue(App.ACCOUNT_NAME, out userName))
                    userName = App.MSISDN;
            }
            else
            {
                ContactInfo cn = UsersTableUtils.getContactInfoFromMSISDN(status.Msisdn);
                userName = cn != null ? cn.Name : status.Msisdn;
            }
            StatusUpdateBox statusUpdateBox = null;
            switch (status.Status_Type)
            {
                case StatusMessage.StatusType.PROFILE_PIC_UPDATE:
                    byte[] statusImageBytes = null;
                    bool isThumbnail;
                    MiscDBUtil.getStatusUpdateImage(status.Msisdn, status.StatusId, out statusImageBytes, out isThumbnail);
                    statusUpdateBox = new ImageStatusUpdate(userName, img, status.Msisdn,
                        UI_Utils.Instance.createImageFromBytes(statusImageBytes), status.Timestamp);
                    if (isThumbnail)
                    {
                        object[] statusObjects = new object[2];
                        statusObjects[0] = status;
                        statusObjects[1] = statusUpdateBox;
                        //url for downloading status image??
                        //string relativeUrl;
                        //AccountUtils.createGetRequest(AccountUtils.BASE + "/" + relativeUrl, onStatusImageDownloaded, true, statusUpdateBox);
                    }

                    break;
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdateBox = new TextStatusUpdate(userName, img, status.Msisdn, status.Message, status.Timestamp);
                    break;
            }
            if (statusBoxTap != null)
            {
                var gl = GestureService.GetGestureListener(statusUpdateBox);
                gl.Tap += statusBoxTap;

            }
            return statusUpdateBox;
        }

        public StatusUpdateBox createStatusUIObject(StatusMessage status, EventHandler<GestureEventArgs> statusBoxTap)
        {
            string userName;            //TODO - GK avoid db hit
            BitmapImage userProfileThumbnail = null; 
            if (App.ViewModel.ConvMap.ContainsKey(status.Msisdn))
            {
                userName = App.ViewModel.ConvMap[status.Msisdn].NameToShow;
                userProfileThumbnail = App.ViewModel.ConvMap[status.Msisdn].AvatarImage;
            }
            else if (App.MSISDN == status.Msisdn)
            {
                if (!App.appSettings.TryGetValue(App.ACCOUNT_NAME, out userName))
                    userName = App.MSISDN;
                userProfileThumbnail = UI_Utils.Instance.getUserProfileThumbnail(HikeConstants.MY_PROFILE_PIC);
            }
            else
            {
                ContactInfo cn = UsersTableUtils.getContactInfoFromMSISDN(status.Msisdn);
                userName = cn != null ? cn.Name : status.Msisdn;
                userProfileThumbnail = UI_Utils.Instance.getUserProfileThumbnail(status.Msisdn);
            }
            StatusUpdateBox statusUpdateBox = null;
            switch (status.Status_Type)
            {
                case StatusMessage.StatusType.PROFILE_PIC_UPDATE:
                    byte[] statusImageBytes = null;
                    bool isThumbnail;
                    MiscDBUtil.getStatusUpdateImage(status.Msisdn, status.StatusId, out statusImageBytes, out isThumbnail);
                    statusUpdateBox = new ImageStatusUpdate(userName, userProfileThumbnail, status.Msisdn,
                        UI_Utils.Instance.createImageFromBytes(statusImageBytes), status.Timestamp);
                    if (isThumbnail)
                    {
                        object[] statusObjects = new object[2];
                        statusObjects[0] = status;
                        statusObjects[1] = statusUpdateBox;
                        //url for downloading status image??
                        //string relativeUrl;
                        //AccountUtils.createGetRequest(AccountUtils.BASE + "/" + relativeUrl, onStatusImageDownloaded, true, statusUpdateBox);
                    }

                    break;
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdateBox = new TextStatusUpdate(userName, userProfileThumbnail, status.Msisdn, status.Message, status.Timestamp);
                    break;
            }
            if (statusBoxTap != null)
            {
                var gl = GestureService.GetGestureListener(statusUpdateBox);
                gl.Tap += statusBoxTap;

            }
            return statusUpdateBox;
        }

        private void onStatusImageDownloaded(byte[] fileBytes, object status)
        {
            object[] vars = status as object[];
            StatusMessage statusMessage = vars[0] as StatusMessage;
            ImageStatusUpdate statusMessageUI = vars[1] as ImageStatusUpdate;
            if (fileBytes != null && fileBytes.Length > 0)
            {
                //TODO move to background thread
                MiscDBUtil.saveStatusImage(statusMessage.Msisdn, statusMessage.StatusId, fileBytes);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    statusMessageUI.StatusImage = UI_Utils.Instance.createImageFromBytes(fileBytes);
                });
            }
        }
    }
}
