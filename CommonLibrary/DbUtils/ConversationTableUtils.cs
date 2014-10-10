using System;
using CommonLibrary.Model;
using System.Collections.Generic;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using System.Windows;
using CommonLibrary.Utils;
using CommonLibrary.Misc;
using CommonLibrary.Languages;
using CommonLibrary.ViewModel;

namespace CommonLibrary.DbUtils
{
    public class ConversationTableUtils
    {
        public static string CONVERSATIONS_DIRECTORY = "CONVERSATIONS";
        private static object readWriteLock = new object();
        /* This function gets all the conversations shown on the message list page*/

        public static ConversationListObject addGroupConversation(ConvMessage convMessage, string groupName)
        {
            ConversationListObject obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message,
                true, convMessage.Timestamp, convMessage.MessageStatus, convMessage.MessageId);

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
            HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
            HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, convs + 1);
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
        public static ConversationListObject addConversation(ConvMessage convMessage, bool isNewGroup, string from = "")
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
                obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message, true, convMessage.Timestamp, convMessage.MessageStatus, convMessage.MessageId);
            }
            else
            {
                if (Utility.IsHikeBotMsg(convMessage.Msisdn))
                {
                    obj = new ConversationListObject(convMessage.Msisdn, Utility.GetHikeBotName(convMessage.Msisdn), convMessage.Message, true, convMessage.Timestamp, convMessage.MessageStatus, convMessage.MessageId);
                }
                else
                {
                    ContactInfo contactInfo = Utility.GetContactInfo(convMessage.Msisdn);
                    obj = new ConversationListObject(convMessage.Msisdn, contactInfo == null ? null : contactInfo.Name, convMessage.Message,
                        contactInfo == null ? !convMessage.IsSms : contactInfo.OnHike, convMessage.Timestamp, convMessage.MessageStatus, convMessage.MessageId);
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
                if (!Utility.IsGroupConversation(from))
                {
                    if (from == HikeInstantiation.MSISDN)
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
                if (!Utility.IsGroupConversation(from))
                {
                    if (from == HikeInstantiation.MSISDN)
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
            HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
            HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, convs + 1);

            return obj;
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
                    HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                    HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_CONVERSATIONS, convs - 1);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConversationTableUtils :: deleteConversation : deleteConversation , Exception : " + ex.StackTrace);
                }
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                ConversationListObject obj = HikeInstantiation.ViewModel.ConvMap[msisdn];
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
            if (!HikeInstantiation.ViewModel.ConvMap.ContainsKey(grpId))
                return false;
            ConversationListObject obj = HikeInstantiation.ViewModel.ConvMap[grpId];
            obj.ContactName = groupName;
            string msisdn = grpId.Replace(":", "_");
            saveConvObject(obj, msisdn);
            //saveConvObjectList();
            return true;
        }

        public static void updateLastMsgStatus(long id, string msisdn, int status)
        {
            if (msisdn == null)
                return;
            ConversationListObject obj = null;
            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                obj = HikeInstantiation.ViewModel.ConvMap[msisdn];
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
            Dictionary<string, ConversationListObject> convMap = HikeInstantiation.ViewModel.ConvMap;

            if (convMap == null)
            {
                if (!HikeInstantiation.IsMarketplace)
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
                                convList = new List<ConversationListObject>(count);

                                for (int i = 0; i < count; i++)
                                {
                                    ConversationListObject item = new ConversationListObject();
                                    try
                                    {
                                            item.Read(reader);
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
                else if (Utility.IsHikeBotMsg(item.Msisdn))
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

        public static List<ConversationListObject> GetConvsFromIndividualFiles()
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
                    if (fileName == "Convs" || fileName == "Convs_bkp" || fileName == "_Convs")
                        continue;

                    using (var file = store.OpenFile(CONVERSATIONS_DIRECTORY + "\\" + fileName, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            ConversationListObject co = new ConversationListObject();
                            co.Read(reader); // we know we have to read from latest file system

                            if (IsValidConv(co))
                                convList.Add(co);
                        }
                    }
                }
            }

            convList.Sort();
            return convList;
        }
    }
}
