﻿using System.Collections.Generic;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using windows_client.Model;
using windows_client.DbUtils;
using Microsoft.Phone.Tasks;
using System.Windows;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.utils;
using Newtonsoft.Json.Linq;

namespace windows_client.View
{
    public partial class GroupInfoPage : PhoneApplicationPage
    {
        private List<GroupMembers> activeGroupMembers;
        private PhotoChooserTask photoChooserTask;
        private string groupId;
        private HikePubSub mPubSub;


        public GroupInfoPage()
        {
            InitializeComponent();
            initPageBasedOnState();
            mPubSub = App.HikePubSubInstance;
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = 95;
            photoChooserTask.PixelWidth = 95;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            BitmapImage groupProfileBitmap = UI_Utils.Instance.getBitMapImage(groupId + "::large");
            if (groupProfileBitmap != null)
            {
                groupImage.Source = groupProfileBitmap;
            }

        }

        private void initPageBasedOnState()
        {
            groupId = PhoneApplicationService.Current.State["objFromChatThreadPage"] as string;
            GroupInfo groupInfo = GroupTableUtils.getGroupInfoForId(groupId);
            this.groupName.Text = groupInfo.GroupName;
            activeGroupMembers = GroupTableUtils.getActiveGroupMembers(groupId);
            this.groupChatParticipants.ItemsSource = activeGroupMembers;
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
                groupImage.Source = image;
                groupImage.Height = 90;
                groupImage.Width = 90;
            }
            else
            {
                Uri uri = new Uri("/View/images/ic_avatar0.png", UriKind.Relative);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
                groupImage.Source = image;
                groupImage.Height = 90;
                groupImage.Width = 90;
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            byte[] buffer = null;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            MemoryStream msLargeImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
            MemoryStream msSmallImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msSmallImage, 35, 35, 0, 95);
            buffer = msSmallImage.ToArray();
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(buffer, new AccountUtils.postResponseFunction(updateProfile_Callback), groupId);

            object[] vals = new object[3];
            vals[0] = groupId;
            vals[1] = msSmallImage;
            vals[2] = msLargeImage;
            mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
        }

        public void updateProfile_Callback(JObject obj)
        {
        }



        private void onGroupProfileTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                photoChooserTask.Show();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("An error occurred.");
            }
        }


    }
}