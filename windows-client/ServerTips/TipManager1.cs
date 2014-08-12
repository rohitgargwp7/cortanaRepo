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


namespace windows_client.ServerTips
{
    class TipManager1
    {
        private static volatile TipManager1 instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new Object(); // this object for reading writing lock

        private static readonly string TIPS_DIRECTORY = "ServerTips";

        public static TipInfo ChatScreenTip { get; set; }

        public static TipInfo MainPageTip { get; set; }

        public static TipManager1 Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new TipManager1();
                            string chatPageTipId = string.Empty;
                            string mainPageTipId = string.Empty;

                            App.appSettings.TryGetValue(HikeConstants.MAIN_PAGE_TIP, out mainPageTipId);

                            if (!String.IsNullOrEmpty(mainPageTipId))
                            {
                                TipInfoBase tempTip = ReadTipFromFile(mainPageTipId);
                                MainPageTip = new TipInfo(tempTip);
                            }

                            App.appSettings.TryGetValue(HikeConstants.CHAT_SCREEN_TIP, out chatPageTipId);

                            if (!String.IsNullOrEmpty(chatPageTipId))
                            {
                                TipInfoBase tempTip = ReadTipFromFile(chatPageTipId);
                                ChatScreenTip = new TipInfo(tempTip);
                            }
                        }
                    }
                }
                return instance;
            }
        }

        public void AddTip(string type, string header, string body, string id)
        {
            TipInfo tempTip = new TipInfo(type, header, body, id);

            if (!IsDuplicate(tempTip.TipId))
            {
                if (tempTip.Location == TipInfo.CHATPAGE)
                {
                    RemoveCurrentTip(ChatScreenTip.TipId);
                    ChatScreenTip = tempTip;
                    App.WriteToIsoStorageSettings(HikeConstants.CHAT_SCREEN_TIP, id);
                    WriteTipToFile(ChatScreenTip);
                }
                else if (tempTip.Location == TipInfo.MAINPAGE)
                {
                    RemoveCurrentTip(MainPageTip.TipId);
                    MainPageTip = tempTip;
                    App.WriteToIsoStorageSettings(HikeConstants.MAIN_PAGE_TIP, id);
                    WriteTipToFile(MainPageTip);
                }
            }
        }

        private bool IsDuplicate(string id)
        {

            if (ChatScreenTip != null && ChatScreenTip.TipId.Equals(id))
                return true;

            if (MainPageTip != null && MainPageTip.TipId.Equals(id))
                return true;

            return false;
        }


        #region Reading and writing to file
        static TipInfoBase ReadTipFromFile(String id)
        {
            TipInfoBase tempTip = new TipInfoBase();
            lock (readWriteLock)
            {
                try
                {
                    string fileName = TIPS_DIRECTORY + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        if (!store.DirectoryExists(TIPS_DIRECTORY))
                            return tempTip;

                        if (!store.FileExists(fileName))
                            return tempTip;

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

                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                tempTip.Write(writer);
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
            }
        }

        public void ClearTips()
        {
            if (ChatScreenTip != null)
                RemoveCurrentTip(ChatScreenTip.TipId);
            if (MainPageTip != null)
                RemoveCurrentTip(MainPageTip.TipId);

            ClearOldTips();

            App.appSettings.Remove(HikeConstants.CHAT_SCREEN_TIP);
            App.appSettings.Remove(HikeConstants.MAIN_PAGE_TIP);
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
