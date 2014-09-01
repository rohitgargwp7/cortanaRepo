using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.IO.IsolatedStorage;
using windows_client.Misc;
using windows_client.Model;

namespace windows_client.utils.ServerTips
{
    /// <summary>
    /// the class for managing server tips
    /// </summary>
    class TipManager
    {
        private static TipManager instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new Object(); // this object for reading writing lock

        private static readonly string TIPS_DIRECTORY = "ServerTips";

        public static TipInfo ChatScreenTip { get; set; }

        public static TipInfo ConversationPageTip { get; set; }

        public static TipManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new TipManager();
                            string chatPageTipId = string.Empty;
                            string mainPageTipId = string.Empty;

                            App.appSettings.TryGetValue(HikeConstants.ServerTips.CONV_PAGE_TIP_ID, out mainPageTipId);

                            if (!String.IsNullOrEmpty(mainPageTipId))
                                ConversationPageTip = ReadTipFromFile(mainPageTipId);

                            App.appSettings.TryGetValue(HikeConstants.ServerTips.CHAT_SCREEN_TIP_ID, out chatPageTipId);

                            if (!String.IsNullOrEmpty(chatPageTipId))
                                ChatScreenTip = ReadTipFromFile(chatPageTipId);

                        }
                    }
                }
                return instance;
            }
        }

        public static ToolTipMode GetToolTipMode(string type)
        {
            switch (type)
            {
                case HikeConstants.ServerTips.STICKER_TIPS:

                    return ToolTipMode.STICKERS;

                case HikeConstants.ServerTips.PROFILE_TIPS:

                    return ToolTipMode.PROFILE_PIC;

                case HikeConstants.ServerTips.ATTACHMENT_TIPS:

                    return ToolTipMode.ATTACHMENTS;

                case HikeConstants.ServerTips.INFORMATIONAL_TIPS:

                    return ToolTipMode.INFORMATIONAL;

                case HikeConstants.ServerTips.FAVOURITE_TIPS:

                    return ToolTipMode.FAVOURITES;

                case HikeConstants.ServerTips.THEME_TIPS:

                    return ToolTipMode.CHAT_THEMES;

                case HikeConstants.ServerTips.INVITATION_TIPS:

                    return ToolTipMode.INVITE_FRIENDS;

                case HikeConstants.ServerTips.STATUS_UPDATE_TIPS:

                    return ToolTipMode.STATUS_UPDATE;

                default:

                    return ToolTipMode.DEFAULT;
            }
        }
        /// <summary>
        /// adding tip and writing to file
        /// </summary>
        /// <param name="type">type of tip</param>
        /// <param name="header">header text</param>
        /// <param name="body">body text</param>
        /// <param name="id">id of the tip</param>
        public void AddTip(string type, string header, string body, string id)
        {

            if (!IsDuplicate(id))
            {
                TipInfo tempTip = new TipInfo(GetToolTipMode(type), header, body, id);

                if (tempTip.IsChatScreenTip)
                {
                    if (ChatScreenTip != null)
                        RemoveTip(ChatScreenTip.TipId);

                    ChatScreenTip = tempTip;
                    App.WriteToIsoStorageSettings(HikeConstants.ServerTips.CHAT_SCREEN_TIP_ID, id);
                }
                else
                {
                    if (ConversationPageTip != null)
                        RemoveTip(ConversationPageTip.TipId);
                    ConversationPageTip = tempTip;
                    App.WriteToIsoStorageSettings(HikeConstants.ServerTips.CONV_PAGE_TIP_ID, id);
                }

                WriteTipToFile(tempTip);

                if (tempTip.IsChatScreenTip)
                    OnChatScreenTipChanged(EventArgs.Empty);
                else
                    OnConversationPageTipChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// checking duplicate packet
        /// </summary>
        /// <param name="id">id of tip</param>
        /// <returns></returns>
        private bool IsDuplicate(string id)
        {

            if (ChatScreenTip != null && ChatScreenTip.TipId.Equals(id))
                return true;

            if (ConversationPageTip != null && ConversationPageTip.TipId.Equals(id))
                return true;

            return false;
        }

        #region Events

        public event EventHandler ConversationPageTipChanged; //event conversation page tip changed
        public event EventHandler ChatScreenTipChanged; //event chat screen tip changed

        void OnConversationPageTipChanged(EventArgs e)
        {
            if (ConversationPageTipChanged != null)
            {
                ConversationPageTipChanged(null, null);
            }
        }

        void OnChatScreenTipChanged(EventArgs e)
        {
            if (ChatScreenTipChanged != null)
            {
                ChatScreenTipChanged(null, null);
            }
        }

        #endregion

        #region Reading and writing to file

        /// <summary>
        /// reading tip from file
        /// </summary>
        /// <param name="id"> id of the tip (comes from appsetting)</param>
        /// <returns></returns>
        static TipInfo ReadTipFromFile(String id)
        {
            TipInfo tempTip = new TipInfo();
            lock (readWriteLock)
            {
                try
                {
                    string fileName = TIPS_DIRECTORY + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        if (!store.DirectoryExists(TIPS_DIRECTORY))
                            return null;

                        if (!store.FileExists(fileName))
                            return null;

                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                tempTip.Read(reader);
                                reader.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Utils:ServerTips:TipManager:ReadTipFromFile :: Read Tip From File, Exception : " + ex.StackTrace);
                }
                return tempTip;
            }
        }

        /// <summary>
        /// writing tip to file
        /// </summary>
        /// <param name="isChatScreenTip"></param>
        static void WriteTipToFile(TipInfo tempTip)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = TIPS_DIRECTORY + "\\" + tempTip.TipId;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(TIPS_DIRECTORY))
                            store.CreateDirectory(TIPS_DIRECTORY);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                tempTip.Write(writer);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Utils:ServerTips:TipManager:AddTip:: Write Tip To File, Exception : " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// removing tip
        /// </summary>
        /// <param name="id">id of the tip to be removed</param>
        public void RemoveTip(string id)
        {
            if (String.IsNullOrEmpty(id))
                return;

            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        string fileName = TIPS_DIRECTORY + "\\" + id;

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Utils:ServerTips:TipManager:RemoveTip :: delete current Tip File, Exception : " + ex.StackTrace);
                    }
                }

                string tempId;
                App.appSettings.TryGetValue(HikeConstants.ServerTips.CHAT_SCREEN_TIP_ID, out tempId);

                if (tempId == id)
                {
                    App.RemoveKeyFromAppSettings(HikeConstants.ServerTips.CHAT_SCREEN_TIP_ID);
                    ChatScreenTip = null;
                }
                else
                {
                    App.RemoveKeyFromAppSettings(HikeConstants.ServerTips.CONV_PAGE_TIP_ID);
                    ConversationPageTip = null;
                }
            }
        }

        /// <summary>
        /// clearing tip reset and unlinking case
        /// </summary>
        public void ClearTips()
        {
            if (ChatScreenTip != null)
                RemoveTip(ChatScreenTip.TipId);

            if (ConversationPageTip != null)
                RemoveTip(ConversationPageTip.TipId);

            ClearOldTips();

            App.appSettings.Remove(HikeConstants.ServerTips.CHAT_SCREEN_TIP_ID);
            App.appSettings.Remove(HikeConstants.ServerTips.CONV_PAGE_TIP_ID);
        }

        /// <summary>
        /// deleting files in servertips directory
        /// </summary>
        public void ClearOldTips()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (!store.DirectoryExists(TIPS_DIRECTORY))
                        return;

                    var fileNames = store.GetFileNames(TIPS_DIRECTORY + "\\*");

                    foreach (var fileName in fileNames)
                    {
                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }

                    store.DeleteDirectory(TIPS_DIRECTORY);

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Utils:ServerTips:TipManager:ClearOldTips::, Exception : " + ex.StackTrace);
                }
            }
        }
        #endregion
    }
}
