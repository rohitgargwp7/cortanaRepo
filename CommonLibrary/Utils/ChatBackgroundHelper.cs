using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using System.IO;
using CommonLibrary.Lib;

namespace CommonLibrary.utils
{
    public class ChatBackgroundHelper
    {
        private const string CHAT_BACKGROUND_DIRECTORY = "ChatBackgrounds";
        private const string bgIdMapFileName = "chatBgMap";

        public Dictionary<String, ChatThemeData> ChatBgMap;

        private static object readWriteLock = new object();
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile ChatBackgroundHelper instance = null;
        private static string[] ChatBackgroundIds = new string[]
        {
            "0","1","2","3","4","7","8","9",
            "10","11","13","14","15","17","18",
            "20","21","22","23","24","25","26","28","29",
            "30","31","32","35","36","37","38","39",
            "40","41"
        };

        public static ChatBackgroundHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new ChatBackgroundHelper();
                        }
                    }
                }
                return instance;
            }
        }

        public ChatBackgroundHelper()
        {
            ChatBgMap = new Dictionary<string, ChatThemeData>();
        }

        public void Instantiate()
        {
            PopulateChatBgMapFromFile();
        }

        /// <summary>
        /// Read Bg Id Map from file
        /// </summary>
        void PopulateChatBgMapFromFile()
        {
            string key = String.Empty;

            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = CHAT_BACKGROUND_DIRECTORY + "\\" + bgIdMapFileName;

                        if (!store.DirectoryExists(CHAT_BACKGROUND_DIRECTORY))
                            return;

                        if (!store.FileExists(fileName))
                            return;

                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                int count = reader.ReadInt32();

                                for (int i = 0; i < count; i++)
                                {
                                    key = reader.ReadString();
                                    var bgImage = new ChatThemeData();
                                    bgImage.Read(reader);
                                    ChatBgMap.Add(key, bgImage);
                                }

                                reader.Close();
                                file.Close();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chat Bg Helper :: Read Bg Id Map From File, Exception : " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Save Background map to file
        /// </summary>
        public async Task SaveChatBgMapToFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = CHAT_BACKGROUND_DIRECTORY + "\\" + bgIdMapFileName;

                        if (!store.DirectoryExists(CHAT_BACKGROUND_DIRECTORY))
                            store.CreateDirectory(CHAT_BACKGROUND_DIRECTORY);

                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);

                                writer.Write(ChatBgMap.Keys.Count);

                                foreach (var key in ChatBgMap.Keys)
                                {
                                    writer.Write(key);
                                    ChatBgMap[key].Write(writer);
                                }

                                writer.Flush();
                                writer.Close();

                                file.Close();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chat Bg Helper :: Write Bg Id Map To File, Exception : " + ex.StackTrace);
                }
            }
        }

        public bool BackgroundIDExists(string bgId)
        {
            return ChatBackgroundIds.Contains(bgId);
        }

        /// <summary>
        /// Called from Netwrok Manager, when user receives change background event
        /// </summary>
        /// <param name="msisdn">msisdn/group id</param>
        /// <param name="bgId">background id</param>
        /// <param name="ts">timestamp at which change was made</param>
        /// <param name="saveMap">make a call to save map to IS</param>
        /// <returns></returns>
        public bool UpdateChatBgMap(string msisdn, string bgId, long ts, bool saveMap = true)
        {
            if (!BackgroundIDExists(bgId))
                return false;

            ChatThemeData bg = ChatBgMap.ContainsKey(msisdn) ? ChatBgMap[msisdn] : new ChatThemeData();

            var newTs = ts;
            var oldTs = bg.Timestamp;

            if (oldTs < newTs)
            {
                bg.BackgroundId = bgId;
                bg.Timestamp = ts;

                ChatBgMap[msisdn] = bg;

                if (saveMap)
                    SaveChatBgMapToFile();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all data on unlink or delete
        /// </summary>
        public void Clear()
        {
            ChatBgMap.Clear();
            SaveChatBgMapToFile();
        }
    }

    public class ChatThemeData
    {
        public string BackgroundId;
        public long Timestamp;

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(BackgroundId);
                writer.Write(Timestamp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BackgroundImage :: Write : Unable To write, Exception : " + ex.StackTrace);
            }
        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                BackgroundId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                Timestamp = reader.ReadInt64();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BackgroundImage :: Read : Read, Exception : " + ex.StackTrace);
            }
        }
    }
}
