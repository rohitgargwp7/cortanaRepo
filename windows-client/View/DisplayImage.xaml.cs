﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using System.IO.IsolatedStorage;
namespace windows_client.View
{
    public partial class DisplayImage : PhoneApplicationPage
    {
        private BitmapImage fileImage;

        double initialAngle;
        double initialScale;

        public DisplayImage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("objectForFileTransfer"))
            {
                object[] fileTapped = (object[])PhoneApplicationService.Current.State["objectForFileTransfer"];
                long messsageId = (long)fileTapped[0];
                string msisdn = (string)fileTapped[1];
                string contentType = (string)fileTapped[2];
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messsageId);

                if (contentType.Contains("Image"))
                {
                    byte[] filebytes;
                    MiscDBUtil.readFileFromIsolatedStorage(filePath, out filebytes);
                    MemoryStream memStream = new MemoryStream(filebytes);
                    memStream.Seek(0, SeekOrigin.Begin);
                    fileImage = new BitmapImage();
                    fileImage.SetSource(memStream);
                    this.FileImage.Source = fileImage;
                }
                else
                {
                    FileImage.Visibility = Visibility.Collapsed;
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

        }

        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            initialAngle = transform.Rotation;
            initialScale = transform.ScaleX;
        }

        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            //transform.Rotation = initialAngle + e.TotalAngleDelta;
            transform.ScaleX = initialScale * e.DistanceRatio;
            transform.ScaleY = initialScale * e.DistanceRatio;
        }

        private void GestureListener_PinchCompleted(object sender, PinchGestureEventArgs e)
        {
            //transform.ScaleX = initialScale;
            //transform.ScaleY = initialScale;
        }
    }
}