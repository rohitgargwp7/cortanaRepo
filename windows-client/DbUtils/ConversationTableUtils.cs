﻿using System;
using windows_client.Model;
using System.Linq;
using System.Collections.Generic;
using windows_client.utils;
using windows_client.View;
using System.Data.Linq;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using windows_client.Languages;
using windows_client.ViewModel;
using Newtonsoft.Json.Linq;

namespace windows_client.DbUtils
{
    public class ConversationTableUtils
    {
        public static string CONVERSATIONS_DIRECTORY = "CONVERSATIONS";
        private static object readWriteLock = new object();
        /* This function gets all the conversations shown on the message list page*/

        public static List<ConversationListObject> getAllConversations()
        {
            List<ConversationListObject> convList = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(CONVERSATIONS_DIRECTORY))
                    return null;
                string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY + "\\*");
                if (files == null || files.Length == 0)
                    return null;
                convList = new List<ConversationListObject>(files.Length);
                foreach (string fileName in files)
                {
                    using (var file = store.OpenFile(CONVERSATIONS_DIRECTORY + "\\" + fileName, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            ConversationListObject co = new ConversationListObject();
                            co.ReadVer_1_0_0_0(reader);
                            if (IsValidConv(co))
                                convList.Add(co);
                        }
                    }
                }
            }
            convList.Sort();
            return convList;
        }

        public static ConversationListObject addGroupConversation(ConvMessage convMessage, string groupName)
        {
            /*
            * Msisdn : GroupId
            * Contactname : GroupOwner
            */
            byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(convMessage.Msisdn);
            ConversationListObject obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message,
                true, convMessage.Timestamp, avatar, convMessage.MessageStatus, convMessage.MessageId);

            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.MEMBERS_JOINED)
            {
                string[] vals = convMessage.Message.Split(';');
                if (vals.Length == 2)
                    obj.LastMessage = vals[1];
                else
                    obj.LastMessage = convMessage.Message;
            }
            string msisdn = obj.Msisdn.Replace(":", "_");
            saveConvObject(obj, msisdn);
            int convs = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, convs + 1);
            //saveNewConv(obj);
            return obj;
        }

        /// <summary>
        /// Creates new conversation object and add it to db
        /// </summary>
        /// <param name="convMessage">Message to be added in conversation</param>
        /// <param name="isNewGroup"></param>
        /// <param name="persistMessage">false if messsage already persisted or to be persisted later</param>
        /// <param name="imageBytes">Avatar image bytes</param>
        /// <param name="from"></param>
        /// <returns></returns>
        public static ConversationListObject addConversation(ConvMessage convMessage, bool isNewGroup, byte[] imageBytes, string from = "")
        {
            ConversationListObject obj = null;
            if (isNewGroup)
            {
                string groupName = convMessage.Msisdn;
                if (PhoneApplicationService.Current.State.ContainsKey(convMessage.Msisdn))
                {
                    groupName = (string)PhoneApplicationService.Current.State[convMessage.Msisdn];
                    PhoneApplicationService.Current.State.Remove(convMessage.Msisdn);
                }
                obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message, true, convMessage.Timestamp, imageBytes, convMessage.MessageStatus, convMessage.MessageId);
            }
            else
            {
                byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(convMessage.Msisdn);
                if (Utils.IsHikeBotMsg(convMessage.Msisdn))
                {
                    obj = new ConversationListObject(convMessage.Msisdn, Utils.GetHikeBotName(convMessage.Msisdn), convMessage.Message, true, convMessage.Timestamp, avatar, convMessage.MessageStatus, convMessage.MessageId);
                }
                else
                {
                    ContactInfo contactInfo = ContactUtils.GetContactInfo(convMessage.Msisdn);
                    obj = new ConversationListObject(convMessage.Msisdn, contactInfo == null ? null : contactInfo.Name, convMessage.Message,
                        contactInfo == null ? !convMessage.IsSms : contactInfo.OnHike, convMessage.Timestamp, avatar, convMessage.MessageStatus, convMessage.MessageId);
                    if (App.ViewModel.Isfavourite(convMessage.Msisdn))
                        obj.IsFav = true;
                }
            }

            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
            {
                obj.LastMessage = convMessage.Message;
            }
            //If ABCD join grp chat convObj should show D joined grp chat as D is last in sorted order
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
            {
                obj.LastMessage = convMessage.Message;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
            {
                obj.LastMessage = String.Format(AppResources.USER_OPTED_IN_MSG, obj.NameToShow);
                convMessage.Message = obj.LastMessage;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CREDITS_GAINED)
            {
                obj.LastMessage = convMessage.Message;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.DND_USER)
            {
                obj.LastMessage = string.Format(AppResources.DND_USER, obj.NameToShow);
                convMessage.Message = obj.LastMessage;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED)
            {
                obj.LastMessage = string.Format(AppResources.USER_JOINED_HIKE, obj.NameToShow);
                convMessage.Message = obj.LastMessage;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_REJOINED)
            {
                obj.LastMessage = string.Format(AppResources.USER_REJOINED_HIKE_TXT, obj.NameToShow);
                convMessage.Message = obj.LastMessage;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED)
            {
                if (!Utils.isGroupConversation(from))
                {
                    if (from == App.MSISDN)
                        convMessage.Message = obj.LastMessage = string.Format(AppResources.ChatBg_Changed_Text, AppResources.You_Txt);
                    else
                        convMessage.Message = obj.LastMessage = string.Format(AppResources.ChatBg_Changed_Text, obj.NameToShow);
                }
                else
                {
                    obj.LastMessage = convMessage.Message;
                }
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGE_NOT_SUPPORTED)
            {
                if (!Utils.isGroupConversation(from))
                {
                    if (from == App.MSISDN)
                        convMessage.Message = obj.LastMessage = string.Format(AppResources.ChatBg_NotChanged_Text, AppResources.You_Txt);
                    else
                        convMessage.Message = obj.LastMessage = string.Format(AppResources.ChatBg_NotChanged_Text, obj.NameToShow);
                }
                else
                {
                    obj.LastMessage = convMessage.Message;
                }
            }

            obj.LastMsgId = convMessage.MessageId;

            //saveNewConv(obj);
            saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
            int convs = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, convs + 1);

            return obj;
        }

        public static void deleteAllConversations()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY + "\\*");
                    if (files != null)
                        foreach (string fileName in files)
                            store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + fileName);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, 0); // clear total number of convs
                }
            }
        }

        public static void deleteConversation(string msisdn)
        {
            msisdn = msisdn.Replace(":", "_");
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (store.FileExists(CONVERSATIONS_DIRECTORY + "\\" + msisdn))
                        store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + msisdn);

                    int convs = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, convs - 1);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConversationTableUtils :: deleteConversation : deleteConversation , Exception : " + ex.StackTrace);
                }
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                ConversationListObject obj = App.ViewModel.ConvMap[msisdn];
                obj.IsOnhike = joined;
                saveConvObject(obj, msisdn);
                //saveConvObjectList();
            }
        }

        public static void updateConversation(ConversationListObject obj)
        {
            saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
            //saveConvObjectList();
        }

        public static bool updateGroupName(string grpId, string groupName)
        {
            if (!App.ViewModel.ConvMap.ContainsKey(grpId))
                return false;
            ConversationListObject obj = App.ViewModel.ConvMap[grpId];
            obj.ContactName = groupName;
            string msisdn = grpId.Replace(":", "_");
            saveConvObject(obj, msisdn);
            //saveConvObjectList();
            return true;
        }

        internal static void updateConversation(List<ContactInfo> cn)
        {
            //saveConvObjectList();

            for (int i = 0; i < cn.Count; i++)
            {
                if (App.ViewModel.ConvMap.ContainsKey(cn[i].Msisdn))
                {
                    ConversationListObject obj = App.ViewModel.ConvMap[cn[i].Msisdn]; //update UI
                    obj.ContactName = cn[i].Name;
                    obj.IsOnhike = cn[i].OnHike;
                    saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
                }
            }
        }

        public static void updateLastMsgStatus(long id, string msisdn, int status)
        {
            if (msisdn == null)
                return;
            ConversationListObject obj = null;
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                obj = App.ViewModel.ConvMap[msisdn];
                if (obj.LastMsgId != id)
                    return;
                if (obj.MessageStatus != ConvMessage.State.UNKNOWN) // no D,R for notification msg so dont update
                {
                    obj.MessageStatus = (ConvMessage.State)status;
                    saveConvObject(obj, msisdn.Replace(":", "_"));
                    //saveConvObjectList();
                }
            }
        }

        public static void saveConvObjectList()
        {
            int convs = 0;
            Dictionary<string, ConversationListObject> convMap = App.ViewModel.ConvMap;

            if (convMap == null)
            {
                if (!App.IS_MARKETPLACE)
                    MessageBox.Show("Map is null !!", "TESTING", MessageBoxButton.OK);
                return;
            }
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string FileName = CONVERSATIONS_DIRECTORY + "\\" + "_Convs";
                    try
                    {
                        if (store.FileExists(FileName))
                            store.DeleteFile(FileName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: saveConvObjectList : DeletingFile , Exception : " + ex.StackTrace);
                    }
                    try
                    {
                        using (var file = store.OpenFile(FileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                if (convMap == null || convMap.Count == 0)
                                    writer.Write(0);
                                else
                                {
                                    writer.Write(convMap.Count);
                                    foreach (ConversationListObject item in convMap.Values)
                                    {
                                        item.Write(writer);
                                        convs++;
                                    }
                                }
                                writer.Flush();
                                writer.Close();
                            }
                            file.Close();
                            file.Dispose();
                        }
                    }

                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: saveConvObjectList : writing file , Exception : " + ex.StackTrace);
                    }

                    try // TODO REVIEW
                    {
                        store.CopyFile(CONVERSATIONS_DIRECTORY + "\\" + "Convs", CONVERSATIONS_DIRECTORY + "\\" + "Convs_bkp", true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: saveConvObjectList : copying file , Exception : " + ex.StackTrace);
                    }
                    try
                    {
                        store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + "Convs");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: saveConvObjectList : SAVE LIST BACKUP , Exception : " + ex.StackTrace);
                        return;
                    }

                    try
                    {
                        store.MoveFile(CONVERSATIONS_DIRECTORY + "\\" + "_Convs", CONVERSATIONS_DIRECTORY + "\\" + "Convs");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: saveConvObjectList : save list, Exception : " + ex.StackTrace);
                    }
                    store.Dispose();
                }
            }
        }

        public static void saveNewConv(ConversationListObject obj)
        {
            int convs = 0;
            Dictionary<string, ConversationListObject> convMap = App.ViewModel.ConvMap;
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string FileName = CONVERSATIONS_DIRECTORY + "\\" + "_Convs";
                    using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            int count = (convMap == null ? 0 : convMap.Count) + 1;
                            writer.Write(count);
                            obj.Write(writer);
                            if (convMap != null && convMap.Count > 0)
                            {
                                foreach (ConversationListObject item in convMap.Values)
                                {
                                    item.Write(writer);
                                    convs++;
                                }
                            }
                        }
                    }
                    store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + "Convs");
                    store.MoveFile(CONVERSATIONS_DIRECTORY + "\\" + "_Convs", CONVERSATIONS_DIRECTORY + "\\" + "Convs");
                }
            }
        }

        /// <summary>
        /// Single conv object is serialized to file
        /// </summary>
        /// <param name="obj"></param>
        public static void saveConvObject(ConversationListObject obj, string msisdn)
        {
            lock (readWriteLock)
            {
                string FileName = CONVERSATIONS_DIRECTORY + "\\" + msisdn;
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            obj.Write(writer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// this function will automatically read which read function version should be used to read file
        /// </summary>
        /// <returns></returns>

        public static List<ConversationListObject> getAllConvs()
        {
            List<ConversationListObject> convList = null;
            // when reading a file , nobody should write it
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(CONVERSATIONS_DIRECTORY))
                        return null;
                    string fname;
                    if (store.FileExists(CONVERSATIONS_DIRECTORY + "\\" + "Convs"))
                        fname = CONVERSATIONS_DIRECTORY + "\\" + "Convs";
                    else if (store.FileExists(CONVERSATIONS_DIRECTORY + "\\" + "Convs_bkp"))
                        fname = CONVERSATIONS_DIRECTORY + "\\" + "Convs_bkp";
                    else
                        return null;

                    using (var file = store.OpenFile(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            int count = 0;
                            try
                            {
                                count = reader.ReadInt32();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("ConversationTableUtils :: getAllConvs : reading count , Exception : " + ex.StackTrace);
                            }
                            if (count > 0)
                            {
                                bool isLessThanEqualTo_1500 = false;
                                if (Utils.compareVersion(App.CURRENT_VERSION, "1.5.0.0") != 1) // current_ver <= 1.5.0.0
                                    isLessThanEqualTo_1500 = true;

                                convList = new List<ConversationListObject>(count);
                                for (int i = 0; i < count; i++)
                                {
                                    ConversationListObject item = new ConversationListObject();
                                    try
                                    {
                                        if (isLessThanEqualTo_1500)
                                            item.ReadVer_1_4_0_0(reader);
                                        else
                                            item.ReadVer_Latest(reader);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("ConversationTableUtils :: getAllConvs : reading file , Exception : " + ex.StackTrace);
                                        item = null;
                                    }
                                    if (IsValidConv(item))
                                        convList.Add(item);
                                }
                                convList.Sort();
                            }
                            reader.Close();
                        }
                        try
                        {
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ConversationTableUtils :: getAllConvs : file dispose, Exception : " + ex.StackTrace);
                        }
                    }
                    store.Dispose();
                }
                return convList;
            }
        }

        // this function will validate the conversation object
        public static bool IsValidConv(ConversationListObject item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.Msisdn))
                    return false;
                else if (Utils.IsHikeBotMsg(item.Msisdn))
                    return true;
                else if (item.Msisdn.Contains(":"))
                {
                    double num;
                    int idx = item.Msisdn.IndexOf(':');
                    if (idx > 0 && double.TryParse(item.Msisdn.Substring(idx + 1), out num))
                        return true;
                    return false;
                }
                else if (item.Msisdn[0] == '+')
                {
                    double num;
                    if (double.TryParse(item.Msisdn.Substring(1), out num))
                        return true;
                    return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationTableUtils :: IsValidConv : IsValidConv, Exception : " + ex.StackTrace);
                return false;
            }
        }


        /* Handle old versions*/
        public static void deleteAllConversationsOld()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string[] files;
                    try
                    {
                        files = store.GetFileNames(CONVERSATIONS_DIRECTORY + "\\*");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: deleteAllConversationsOld : get file Names, Exception : " + ex.StackTrace);
                        files = null;
                    }

                    if (files == null)
                        return;
                    foreach (string fileName in files)
                    {
                        try
                        {
                            if (fileName == "Convs")
                                continue;
                            store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + fileName);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("File {0} does not exist.", CONVERSATIONS_DIRECTORY + "\\" + fileName);
                            Debug.WriteLine("ConversationTableUtils :: deleteAllConversationsOld : delete file, Exception : " + ex.StackTrace);
                        }

                    }
                }
            }
        }

        /// <summary>
        /// This functions reads convMap and then store each converstaion in an individual file
        /// </summary>
        /// <param name="cObjList"></param>
        public static void saveConvObjectListIndividual(List<ConversationListObject> cObjList)
        {
            if (cObjList == null)
                return;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                for (int i = 0; i < cObjList.Count; i++)
                {
                    string FileName = CONVERSATIONS_DIRECTORY + "\\" + cObjList[i].Msisdn.Replace(":", "_");
                    using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            cObjList[i].Write(writer);
                        }
                    }
                }
            }
        }

        public static List<ConversationListObject> GetConvsFromIndividualFiles()
        {
            byte[] data = null;
            List<ConversationListObject> convList = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(CONVERSATIONS_DIRECTORY))
                    return null;
                string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY + "\\*");
                if (files == null || files.Length == 0)
                    return null;
                convList = new List<ConversationListObject>(files.Length);
                foreach (string fileName in files)
                {
                    if (fileName == "Convs" || fileName == "Convs_bkp" || fileName == "_Convs")
                        continue;
                    using (var file = store.OpenFile(CONVERSATIONS_DIRECTORY + "\\" + fileName, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            ConversationListObject co = new ConversationListObject();
                            co.ReadVer_Latest(reader); // we know we have to read from latest file system
                            if (IsValidConv(co))
                                convList.Add(co);
                        }
                    }
                }
            }
            convList.Sort();
            return convList;
        }


        /// <summary>
        /// Function for setting second last Message as LastMessage For ConvObject 
        /// ConvObj has not been updated in file in this function
        /// </summary>
        /// <param name="msisdn"></param>
        /// <param name="obj"></param>
        public static void SetSecondLastMessageForConvObject(string msisdn,ConversationListObject obj)
        {
            ConvMessage cm = MessagesTableUtils.getSecondLastMessageForMsisdn(msisdn);

            if (cm == null)
            {
                obj.LastMessage = String.Empty;
                obj.MessageStatus = ConvMessage.State.UNKNOWN;
            }
            else
            {
                obj.LastMessage = cm.Message;
                obj.LastMsgId = cm.MessageId;
                obj.MessageStatus = cm.MessageStatus;

                if (cm.FileAttachment != null)
                {
                    if (cm.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                        obj.LastMessage = AppResources.Image_Txt;
                    else if (cm.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                        obj.LastMessage = AppResources.Audio_Txt;
                    else if (cm.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                        obj.LastMessage = AppResources.Video_Txt;
                    else if (cm.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        obj.LastMessage = AppResources.ContactTransfer_Text;
                    else if (cm.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                        obj.LastMessage = AppResources.Location_Txt;
                    else
                        obj.LastMessage = AppResources.UnknownFile_txt;

                    obj.TimeStamp = cm.Timestamp;
                }
                else // check here nudge , notification , status update
                {
                    if (!String.IsNullOrEmpty(cm.MetaDataString))
                    {
                        // NUDGE
                        if (cm.MetaDataString.Contains(HikeConstants.POKE))
                            obj.LastMessage = AppResources.Nudge;
                        else // NOTIFICATION AND NORMAL MSGS
                            obj.LastMessage = cm.Message;
                    }
                }
            }
        }
    }
}
