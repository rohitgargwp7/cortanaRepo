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
using Microsoft.Xna.Framework.Media;
using System.IO;
using windows_client.Languages;
using System.Diagnostics;

namespace windows_client
{
    public class FileTransfer
    {
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        private static Dictionary<string, ConvMessage> requestIdConvMsgMap = new Dictionary<string, ConvMessage>();

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

        public void downloadFile(ConvMessage conMessage, string msisdn)
        {
            Uri downloadUriSource = new Uri(Uri.EscapeUriString(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + conMessage.FileAttachment.FileKey),
                UriKind.RelativeOrAbsolute);

            string relativeFilePath = "/" + msisdn + "/" + Convert.ToString(conMessage.MessageId);
            string destinationPath = "shared/transfers" + "/" + Convert.ToString(conMessage.MessageId);
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
                requestIdConvMsgMap.Add(transferRequest.RequestId, conMessage);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine("FileTransfer :: downloadFile : downloadFile, Exception : " + ex.StackTrace);
                MessageBox.Show(AppResources.FileTransfer_ErrorMsgBoxText + ex.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine("FileTransfer :: downloadFile : downloadFile, Exception : " + e.StackTrace);
                MessageBox.Show(AppResources.FileTransfer_ErrorMsgBoxText);
            }
        }

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
                        ConvMessage convMessage;
                        requestIdConvMsgMap.TryGetValue(transfer.RequestId, out convMessage);
                        //todo:
                        if (convMessage != null)
                            convMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
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

                                if (convMessage != null && convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                                {
                                    IsolatedStorageFileStream myFileStream = isoStore.OpenFile(destinationPath, FileMode.Open, FileAccess.Read);
                                    MediaLibrary library = new MediaLibrary();
                                    myFileStream.Seek(0, 0);
                                    library.SavePicture(convMessage.FileAttachment.FileName, myFileStream);
                                }
                                var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                                if (currentPage != null)
                                {
                                    currentPage.displayAttachment(convMessage, true);
                                }
                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        try
                        {
                            RemoveTransferRequest(transfer.RequestId);
                            // This is where you can handle whatever error is indicated by the
                            // StatusCode and then remove the transfer from the queue. 
                            if (transfer.TransferError != null)
                            {
                                // Handle TransferError if one exists.
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("FileTransfer :: process transfer : process transfer, Exception : " + ex.StackTrace);
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
            ConvMessage convMessage;
            requestIdConvMsgMap.TryGetValue(e.Request.RequestId, out convMessage);
            if (convMessage != null)
            {
                if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.CANCELED)
                {
                    convMessage.ProgressBarValue = e.Request.BytesReceived * 100 / e.Request.TotalBytesToReceive;
                }
                else
                {
                    try
                    {
                        BackgroundTransferRequest transferRequest = BackgroundTransferService.Find(e.Request.RequestId);
                        BackgroundTransferService.Remove(transferRequest);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("FileTransfer :: transfer_TransferProgressChanged: transfer_TransferProgressChanged, Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        private void RemoveTransferRequest(string transferID)
        {
            requestIdConvMsgMap.Remove(transferID);
            // Use Find to retrieve the transfer request with the specified ID.
            BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);
            try
            {
                BackgroundTransferService.Remove(transferToRemove);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FileTransfer :: RemoveTransferRequest : process transfer, Exception : " + ex.StackTrace);
            }
        }

    }
}
