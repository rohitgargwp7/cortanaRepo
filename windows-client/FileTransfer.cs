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
using System.Collections.Concurrent;

namespace windows_client
{
    public class FileTransfer
    {
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;
        IEnumerable<BackgroundTransferRequest> transferRequests;
        private static ConcurrentDictionary<string, ConvMessage> msgIdConvMsgMap = new ConcurrentDictionary<string, ConvMessage>();

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
                        {
                            instance = new FileTransfer();
                            instance.ProcessOldTransferRequests();
                        }
                    }
                }
                return instance;
            }
        }

        public void DownloadFile(ConvMessage conMessage, string msisdn)
        {
            if (conMessage == null || string.IsNullOrEmpty(msisdn) || conMessage.FileAttachment == null)
                return;
            Uri downloadUriSource = new Uri(Uri.EscapeUriString(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + conMessage.FileAttachment.FileKey),
                UriKind.RelativeOrAbsolute);

            string relativeFilePath = msisdn + "/" + conMessage.MessageId;
            string destinationPath = "shared/transfers" + "/" + conMessage.MessageId;
            Uri destinationUri = new Uri(destinationPath, UriKind.RelativeOrAbsolute);

            BackgroundTransferRequest transferRequest = new BackgroundTransferRequest(downloadUriSource);

            // Set the transfer method. GET and POST are supported.
            transferRequest.Tag = relativeFilePath;
            transferRequest.Method = "GET";
            transferRequest.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
            transferRequest.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
            transferRequest.DownloadLocation = destinationUri;
            bool addedToDownload = false;
            try
            {

                transferRequest.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
                if (msgIdConvMsgMap.Count < 25)//max 25 downloads in queue
                {
                    if (!msgIdConvMsgMap.ContainsKey(transferRequest.Tag))
                    {
                        BackgroundTransferService.Add(transferRequest);
                        msgIdConvMsgMap.TryAdd(transferRequest.Tag, conMessage);
                        addedToDownload = true;
                    }
                    else
                    {
                        Debug.WriteLine("Already added to queue, TAG:" + transferRequest.Tag);
                    }
                }
                else
                {
                    if (conMessage.UserTappedDownload)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                MessageBox.Show("More than 25 files cannot be downloaded at a time");
                            });
                    }
                }
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
            finally
            {
                if (!addedToDownload)
                {
                    conMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                    MiscDBUtil.UpdateFileAttachmentState(msisdn, conMessage.MessageId.ToString(), Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                }
            }
        }

        private void ProcessTransfer(BackgroundTransferRequest transfer)
        {
            switch (transfer.TransferStatus)
            {
                case TransferStatus.Completed:
                    ConvMessage convMessage;
                    msgIdConvMsgMap.TryGetValue(transfer.Tag, out convMessage);
                    if (RemoveTransferRequest(transfer, transfer.Tag) && transfer.TransferError == null && (transfer.StatusCode == 200 || transfer.StatusCode == 206))
                    {
                        if (convMessage != null)
                            convMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);

                        if (transfer.UploadLocation == null)
                        {
                            try
                            {
                                string[] data = transfer.Tag.Split('/');
                                if (data.Length == 2)
                                {
                                    string msisdn = data[0];
                                    string messageId = data[1];
                                    string destinationPath = HikeConstants.FILES_BYTE_LOCATION + "/" + transfer.Tag;
                                    string destinationDirectory = destinationPath.Substring(0, destinationPath.LastIndexOf("/"));


                                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                                    {
                                        if (isoStore.FileExists(destinationPath))
                                            isoStore.DeleteFile(destinationPath);

                                        if (!isoStore.DirectoryExists(destinationDirectory))
                                            isoStore.CreateDirectory(destinationDirectory);

                                        isoStore.MoveFile(transfer.DownloadLocation.OriginalString, destinationPath);
                                        if (isoStore.FileExists(transfer.DownloadLocation.OriginalString))
                                            isoStore.DeleteFile(transfer.DownloadLocation.OriginalString);
                                        if (convMessage != null && convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                                        {
                                            IsolatedStorageFileStream myFileStream = isoStore.OpenFile(destinationPath, FileMode.Open, FileAccess.Read);
                                            MediaLibrary library = new MediaLibrary();
                                            myFileStream.Seek(0, 0);
                                            library.SavePicture(convMessage.FileAttachment.FileName, myFileStream);
                                        }
                                    }

                                    MiscDBUtil.UpdateFileAttachmentState(msisdn, messageId, Attachment.AttachmentState.COMPLETED);
                                    if (convMessage != null)
                                    {
                                        var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                                        if (currentPage != null && convMessage.UserTappedDownload)
                                        {
                                            currentPage.displayAttachment(convMessage);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("SOURCE:{0},\nEXCEPTION:{1},\nTAG:{2},\nFILEKEY:{3}", transfer.DownloadLocation.OriginalString, ex.Message, transfer.Tag, transfer.RequestUri.ToString());
                            }
                        }
                    }
                    else
                    {
                        if (convMessage != null)
                            convMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                        string[] data = transfer.Tag.Split('/');
                        if (data.Length == 2)
                        {
                            string msisdn = data[0];
                            string messageId = data[1];
                            MiscDBUtil.UpdateFileAttachmentState(msisdn, messageId, Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                        }
                        try
                        {
                            //case file doesn't exists on server
                            if (transfer.StatusCode == 400)
                            {

                                var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                                if (currentPage != null)
                                {
                                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                                        {
                                            MessageBox.Show(AppResources.File_Not_Exist_Message, AppResources.File_Not_Exist_Caption, MessageBoxButton.OK);
                                        });
                                }
                            }
                            // This is where you can handle whatever error is indicated by the
                            // StatusCode and then remove the transfer from the queue. 
                            if (transfer.TransferError != null)
                            {
                                Debug.WriteLine("Error occured in file transfer,Exception:", transfer.TransferError.Message);
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
            msgIdConvMsgMap.TryGetValue(e.Request.Tag, out convMessage);
            if (convMessage != null)
            {
                if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.CANCELED)
                {
                    convMessage.ProgressBarValue = e.Request.BytesReceived * 100 / e.Request.TotalBytesToReceive;
                    convMessage.ProgressText = string.Format("{0} of {1}", Utils.ConvertToStorageSizeString(e.Request.BytesReceived), Utils.ConvertToStorageSizeString(e.Request.TotalBytesToReceive));
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

        public void ProcessOldTransferRequests()
        {
            if (transferRequests != null)
            {
                foreach (var request in transferRequests)
                {
                    request.Dispose();
                }
            }

            transferRequests = BackgroundTransferService.Requests;

            foreach (var transfer in transferRequests)
            {
                msgIdConvMsgMap[transfer.Tag] = null;
                transfer.TransferStatusChanged -= transfer_TransferStatusChanged;
                transfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);

                transfer.TransferProgressChanged -= transfer_TransferProgressChanged;
                transfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);

                ProcessTransfer(transfer);
            }

            //if (WaitingForExternalPower)
            //{
            //    MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power to continue transferring.");
            //}
            //if (WaitingForExternalPowerDueToBatterySaverMode)
            //{
            //    MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or disable Battery Saver Mode to continue transferring.");
            //}
            //if (WaitingForNonVoiceBlockingNetwork)
            //{
            //    MessageBox.Show("You have one or more file transfers waiting for a network that supports simultaneous voice and data.");
            //}
            //if (WaitingForWiFi)
            //{
            //    MessageBox.Show("You have one or more file transfers waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring.");
            //}
        }

        public void RemoveAllTransferRequests()
        {
            IEnumerable<BackgroundTransferRequest> transferRequests = BackgroundTransferService.Requests;

            foreach (var transfer in transferRequests)
            {
                RemoveTransferRequest(transfer, transfer.Tag);
            }
        }

        private bool RemoveTransferRequest(BackgroundTransferRequest transferToRemove, string mapKey)
        {
            ConvMessage conv;
            msgIdConvMsgMap.TryRemove(mapKey, out conv);
            bool isSucess = false;
            // Use Find to retrieve the transfer request with the specified ID.
            try
            {
                BackgroundTransferService.Remove(transferToRemove);
                isSucess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FileTransfer :: RemoveTransferRequest : process transfer, Exception :{0}, StackTrace:{1} ", ex.Message, ex.StackTrace);
            }
            return isSucess;
        }

        public void UpdateConvMap(ConvMessage convMessage, string msisdn)
        {
            string key = msisdn + "/" + convMessage.MessageId.ToString(); ;
            if (msgIdConvMsgMap.ContainsKey(key))
            {
                msgIdConvMsgMap[key] = convMessage;
            }
        }

        public ConvMessage GetDownloadingMessage(string msisdn, string msgId, out bool containsMsg)
        {
            ConvMessage downladMessage;
            containsMsg = msgIdConvMsgMap.TryGetValue(msisdn + "/" + msgId, out downladMessage);
            return downladMessage;
        }

    }
}
