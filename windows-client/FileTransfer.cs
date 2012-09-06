﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.BackgroundTransfer;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using windows_client.Model;
using windows_client.utils;

namespace windows_client
{
    public class FileTransfer
    {
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        private static volatile FileTransfer instance = null;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        public static FileTransfer Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new FileTransfer();
                    }
                }
                return instance;
            }
        }

        //IEnumerable<BackgroundTransferRequest> transferRequests;

        //public FileTransfer()
        //{
        //    transferRequests = BackgroundTransferService.Requests;
        //}

        public void downloadFile(string fileKey, long messageId)
        {
            Uri downloadUriSource = new Uri(Uri.EscapeUriString(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + fileKey), UriKind.RelativeOrAbsolute);

            string relativeFilePath = "/" + Convert.ToString(messageId) + "large";
            string destinationPath = "shared/transfers" + relativeFilePath;
            Uri destinationUri = new Uri(destinationPath, UriKind.RelativeOrAbsolute);

            BackgroundTransferRequest transferRequest = new BackgroundTransferRequest(downloadUriSource);

            // Set the transfer method. GET and POST are supported.
            transferRequest.Tag = relativeFilePath;
            transferRequest.Method = "GET";
            transferRequest.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
            transferRequest.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
            transferRequest.DownloadLocation = destinationUri;
            try
            {
                BackgroundTransferService.Add(transferRequest);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Unable to add background transfer request. " + ex.Message);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to add background transfer request.");
            }
        }



        //public void uploadFile(string fileName)
        //{
        //    Uri uploadUriTarget = new Uri(Uri.EscapeUriString(HikeConstants.FILE_TRANSFER_BASE_URL), UriKind.Absolute);

        //    string relativeFilePath = "/" + fileName;
        //    string sourcePath = "shared/transfers" + relativeFilePath;
        //    Uri sourceUri = new Uri("/" + sourcePath, UriKind.Relative);

        //    Uri downloadResponse = new Uri("shared/transfers/response", UriKind.RelativeOrAbsolute);

        //    BackgroundTransferRequest transferRequest = new BackgroundTransferRequest(uploadUriTarget);


        //    byte[] dataToSend;
        //    Attachment.readFileFromIsolatedStorage(sourcePath, out dataToSend);



        //    // Set the transfer method. GET and POST are supported.
        //    transferRequest.Tag = relativeFilePath;
        //    transferRequest.Method = "POST";
        //    transferRequest.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
        //    transferRequest.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
        //    //transferRequest.DownloadLocation = downloadResponse;
        //    transferRequest.Headers["Content-Name"] = "ic_phone_big.png";
        //    transferRequest.Headers["Cookie"] = "user=" + AccountUtils.mToken;
        //    transferRequest.Headers["Content-Type"] = "";
        //    transferRequest.Headers["Connection"] = "Keep-Alive";
        //    transferRequest.Headers["X-Thumbnail-Required"] = "0";

        //    transferRequest.UploadLocation = sourceUri;

        //    try
        //    {
        //        BackgroundTransferService.Add(transferRequest);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        MessageBox.Show("Unable to add background transfer request. " + ex.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show("Unable to add background transfer request.");
        //    }
        //}


        private void ProcessTransfer(BackgroundTransferRequest transfer)
        {
            switch (transfer.TransferStatus)
            {
                case TransferStatus.Completed:

                    //using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    //{
                    //    string responsePath = "shared/transfers/response";
                    //    if (isoStore.FileExists(responsePath))
                    //    {
                    //        byte[] responseFromServer;
                    //        Attachment.readFileFromIsolatedStorage(responsePath, out responseFromServer);
                    //        string responseString = System.Text.Encoding.UTF8.GetString(responseFromServer, 0, responseFromServer.Length);
                    //    }
                    //}
                    // If the status code of a completed transfer is 200 or 206, the
                    // transfer was successful
                    if (transfer.StatusCode == 200 || transfer.StatusCode == 206)
                    {
                        // Remove the transfer request in order to make room in the 
                        // queue for more transfers. Transfers are not automatically
                        // removed by the system.
                        RemoveTransferRequest(transfer.RequestId);
                        // In this example, the downloaded file is moved into the root
                        // Isolated Storage directory
                        if (transfer.UploadLocation == null)
                        {
                            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                string destinationPath = HikeConstants.FILE_TRANSFER_LOCATION + transfer.Tag;
                                if (isoStore.FileExists(destinationPath))
                                {
                                    isoStore.DeleteFile(destinationPath);
                                }
                                isoStore.MoveFile(transfer.DownloadLocation.OriginalString, destinationPath);
                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        // This is where you can handle whatever error is indicated by the
                        // StatusCode and then remove the transfer from the queue. 
                        RemoveTransferRequest(transfer.RequestId);
                        if (transfer.TransferError != null)
                        {
                            // Handle TransferError if one exists.
                        }
                    }
                    break;

                case TransferStatus.WaitingForExternalPower:
                    WaitingForExternalPower = true;
                    break;

                case TransferStatus.WaitingForExternalPowerDueToBatterySaverMode:
                    WaitingForExternalPowerDueToBatterySaverMode = true;
                    break;

                case TransferStatus.WaitingForNonVoiceBlockingNetwork:
                    WaitingForNonVoiceBlockingNetwork = true;
                    break;

                case TransferStatus.WaitingForWiFi:
                    WaitingForWiFi = true;
                    break;
            }
        }

        void transfer_TransferStatusChanged(object sender, BackgroundTransferEventArgs e)
        {
            ProcessTransfer(e.Request);
            //   UpdateUI();
        }

        void transfer_TransferProgressChanged(object sender, BackgroundTransferEventArgs e)
        {
            // UpdateUI();
        }

        private void RemoveTransferRequest(string transferID)
        {
            // Use Find to retrieve the transfer request with the specified ID.
            BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);

            // Try to remove the transfer from the background transfer service.
            try
            {
                BackgroundTransferService.Remove(transferToRemove);
            }
            catch (Exception e)
            {
                // Handle the exception.
            }
        }

    }
}
