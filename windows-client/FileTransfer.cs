using System;
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
using windows_client.Controls;
using windows_client.View;
using windows_client.DbUtils;

namespace windows_client
{
    public class FileTransfer
    {
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        private static Dictionary<string, ReceivedChatBubble> requestIdChatBubbleMap = new Dictionary<string, ReceivedChatBubble>();

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

        public void downloadFile(MyChatBubble chatBubble, string msisdn)
        {
            Uri downloadUriSource = new Uri(Uri.EscapeUriString(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + chatBubble.FileAttachment.FileKey), 
                UriKind.RelativeOrAbsolute);

            string relativeFilePath = "/" + msisdn + "/" + Convert.ToString(chatBubble.MessageId);
            string destinationPath = "shared/transfers" + "/" + Convert.ToString(chatBubble.MessageId);
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
                transferRequest.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
                BackgroundTransferService.Add(transferRequest);
                requestIdChatBubbleMap.Add(transferRequest.RequestId, chatBubble as ReceivedChatBubble);
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
                    if (transfer.StatusCode == 200 || transfer.StatusCode == 206)
                    {
                        // Remove the transfer request in order to make room in the 
                        // queue for more transfers. Transfers are not automatically
                        // removed by the system.
                        ReceivedChatBubble chatBubble;
                        requestIdChatBubbleMap.TryGetValue(transfer.RequestId, out chatBubble);
                        chatBubble.setAttachmentState(Attachment.AttachmentState.COMPLETED);
                        RemoveTransferRequest(transfer.RequestId);
                        //RemoveTransferRequest(transfer.RequestId);
                        // In this example, the downloaded file is moved into the root
                        // Isolated Storage directory
                        if (transfer.UploadLocation == null)
                        {
                            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                string destinationPath = HikeConstants.FILES_BYTE_LOCATION + transfer.Tag;
                                string destinationDirectory = destinationPath.Substring(0, destinationPath.LastIndexOf("/"));
                                if (isoStore.FileExists(destinationPath))
                                {
                                    isoStore.DeleteFile(destinationPath);
                                }
                                if (!isoStore.DirectoryExists(destinationDirectory))
                                {
                                    isoStore.CreateDirectory(destinationDirectory);
                                }
                                isoStore.MoveFile(transfer.DownloadLocation.OriginalString, destinationPath);
                                isoStore.DeleteFile(transfer.DownloadLocation.OriginalString);

                                var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                                if (currentPage != null)
                                {
                                    currentPage.displayAttachment(chatBubble, true);
                                }

                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        RemoveTransferRequest(transfer.RequestId);
                        // This is where you can handle whatever error is indicated by the
                        // StatusCode and then remove the transfer from the queue. 
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
        }

        void transfer_TransferProgressChanged(object sender, BackgroundTransferEventArgs e)
        {
            ReceivedChatBubble chatBubble;
            requestIdChatBubbleMap.TryGetValue(e.Request.RequestId, out chatBubble);
            chatBubble.updateProgress(e.Request.BytesReceived * 100 / e.Request.TotalBytesToReceive);
        }

        private void RemoveTransferRequest(string transferID)
        {
            requestIdChatBubbleMap.Remove(transferID);
            // Use Find to retrieve the transfer request with the specified ID.
            BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);
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
