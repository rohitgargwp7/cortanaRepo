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


namespace windows_client.utils.ServerTips
{
    class TipManager
    {
        private static volatile TipManager instance = null;

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

                            App.appSettings.TryGetValue(HikeConstants.ServerTips.CONV_PAGE_TIP, out mainPageTipId);

                            if (!String.IsNullOrEmpty(mainPageTipId))
                                ConversationPageTip = ReadTipFromFile(mainPageTipId);

                            App.appSettings.TryGetValue(HikeConstants.ServerTips.CHAT_SCREEN_TIP, out chatPageTipId);

                            if (!String.IsNullOrEmpty(chatPageTipId))
                                ChatScreenTip = ReadTipFromFile(chatPageTipId);

                        }
                    }
                }
                return instance;
            }
        }

        public void AddTip(string type, string header, string body, string id)
        {

            if (!IsDuplicate(id))
            {
                TipInfo tempTip = new TipInfo(type, header, body, id);
                string tempLocation = tempTip.GetLocation();

                if (tempLocation == HikeConstants.ServerTips.CHAT_SCREEN_TIP)
                {
                    if (ChatScreenTip != null)
                        RemoveCurrentTip(ChatScreenTip.TipId);
                    ChatScreenTip = tempTip;
                }
                else
                {
                    if (ConversationPageTip != null)
                        RemoveCurrentTip(ConversationPageTip.TipId);
                    ConversationPageTip = tempTip;
                }

                WriteTipToFile(tempLocation);
                App.WriteToIsoStorageSettings(tempLocation, id);

                if (tempLocation == HikeConstants.ServerTips.CONV_PAGE_TIP)
                    OnConversationPageTipChanged(EventArgs.Empty);
                else
                    OnChatScreenTipChanged(EventArgs.Empty);
            }
        }

        private bool IsDuplicate(string id)
        {

            if (ChatScreenTip != null && ChatScreenTip.TipId.Equals(id))
                return true;

            if (ConversationPageTip != null && ConversationPageTip.TipId.Equals(id))
                return true;

            return false;
        }

        #region Events

        public event EventHandler ConversationPageTipChanged;
        public event EventHandler ChatScreenTipChanged;

        protected virtual void OnConversationPageTipChanged(EventArgs e)
        {
            EventHandler handler = ConversationPageTipChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void OnChatScreenTipChanged(EventArgs e)
        {
            EventHandler handler = ChatScreenTipChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Reading and writing to file
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
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Read ProTip From File, Exception : " + ex.StackTrace);
                }
                return tempTip;
            }
        }

        static void WriteTipToFile(string tempPos)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = TIPS_DIRECTORY + "\\" + (tempPos == HikeConstants.ServerTips.CHAT_SCREEN_TIP ? ChatScreenTip.TipId : ConversationPageTip.TipId);
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(TIPS_DIRECTORY))
                            store.CreateDirectory(TIPS_DIRECTORY);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);

                                if (tempPos == HikeConstants.ServerTips.CHAT_SCREEN_TIP)
                                    ChatScreenTip.Write(writer);
                                else
                                    ConversationPageTip.Write(writer);

                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Write ProTip To File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void RemoveCurrentTip(string id)
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
                        System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete current ProTip File, Exception : " + ex.StackTrace);
                    }
                }

                string tempId;
                App.appSettings.TryGetValue(HikeConstants.ServerTips.CHAT_SCREEN_TIP, out tempId);
                if (tempId == id)
                {
                    App.RemoveKeyFromAppSettings(HikeConstants.ServerTips.CHAT_SCREEN_TIP);
                    ChatScreenTip = null;
                }
                else
                {
                    App.RemoveKeyFromAppSettings(HikeConstants.ServerTips.CONV_PAGE_TIP);
                    ConversationPageTip = null;
                }
            }
        }

        public void ClearTips()
        {
            if (ChatScreenTip != null)
                RemoveCurrentTip(ChatScreenTip.TipId);
            if (ConversationPageTip != null)
                RemoveCurrentTip(ConversationPageTip.TipId);

            ClearOldTips();

            App.appSettings.Remove(HikeConstants.ServerTips.CHAT_SCREEN_TIP);
            App.appSettings.Remove(HikeConstants.ServerTips.CONV_PAGE_TIP);
        }

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

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete ProTip Data on upgrade to 2.2.2.1, Exception : " + ex.StackTrace);
                }
            }
        }
        #endregion
    }
}
