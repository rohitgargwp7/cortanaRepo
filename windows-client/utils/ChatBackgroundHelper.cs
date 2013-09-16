using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using System.IO;
using windows_client.Misc;
using System.Windows.Media.Imaging;

namespace windows_client.utils
{
    public class ChatBackgroundHelper
    {
        private const string CHAT_BACKGROUND_DIRECTORY = "ChatBackgrounds";

        private const string bgIdsListFileName = "chatBgList";
        private const string bgIdMapFileName = "chatBgMap";

        Dictionary<String, ChatBackground> ChatBgCache;
        List<String> BackgroundIdList;

        public List<ChatBackground> BackgroundList;

        public Dictionary<String, BackgroundImage> ChatBgMap;

        private static object readWriteLock = new object();
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile ChatBackgroundHelper instance = null;

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
            ChatBgCache = new Dictionary<string, ChatBackground>();
            ChatBgMap = new Dictionary<string, BackgroundImage>();
            BackgroundIdList = new List<string>();
            BackgroundList = new List<ChatBackground>();
        }

        public void Instantiate()
        {
            BackgroundIdList.Clear();

            PopulateBackgrounds();
            PopulateFromFile();

            foreach (var id in BackgroundIdList)
            {
                BackgroundList.Add(ChatBgCache[id]);
            }

            BackgroundList.Sort(CompareBackground);
        }

        public void UpdateChatBgMap(string id, string bgId, string image, string ts)
        {
            ChatBgMap[id]=new BackgroundImage()
            {
                BackgroundId = bgId,
                BackgroundImageBase64 = image,
                Timestamp = ts
            };

            SaveMapToFile();
        }

        /// <summary>
        /// Read Bg Id Map from file
        /// </summary>
        void PopulateFromFile()
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
                                    var bgImage = new BackgroundImage();
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
        async Task SaveMapToFile()
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

        void PopulateBackgrounds()
        {
            ReadDefaultIdsFromFile();

            foreach (var id in BackgroundIdList)
            {
                var chatBg = ReadBackgroundFromFile(id);

                ChatBgCache.Add(id, chatBg);
            }
        }

        Random random = new Random();

        public void SetSelectedChatBackgorund(string msisdn, string bgId, string image)
        {
            App.ViewModel.SelectedBackground = ChatBgCache[bgId];

            ChatBgMap[msisdn] = new BackgroundImage()
            {
                BackgroundId = bgId,
                BackgroundImageBase64 = image,
                Timestamp = TimeUtils.getCurrentTimeStamp().ToString()
            };

            SaveMapToFile();
        }

        public void SetSelectedChatBackgorund(string msisdn)
        {
            BackgroundImage bgObj;
          
            if (ChatBgMap.TryGetValue(msisdn, out bgObj))
            {
                ChatBackground bg;

                if (ChatBgCache.TryGetValue(bgObj.BackgroundId, out bg))
                    App.ViewModel.SelectedBackground = bg;
                else
                {
                    bg = ReadBackgroundFromFile(bgObj.BackgroundId);

                    if (bg == null)
                        GetNewBackground(msisdn, bgObj.BackgroundId);
                    else
                        ChatBgCache[bgObj.BackgroundId] = App.ViewModel.SelectedBackground = bg;
                }
            }
            else
            {
                ChatBgMap[msisdn] = new BackgroundImage()
                {
                    BackgroundId = SetDefaultBackground(msisdn),
                    Timestamp = TimeUtils.getCurrentTimeStamp().ToString()
                };

                SaveMapToFile();
            }
        }

        String SetDefaultBackground(string msisdn)
        {
            int index = random.Next(0, BackgroundIdList.Count);
            var id = BackgroundIdList[index];

            App.ViewModel.SelectedBackground = ChatBgCache[id];
            return id;
        }

        /// <summary>
        /// Get Background from file
        /// </summary>
        /// <param name="id">The background Id for which background is needed</param>
        /// <returns></returns>
        ChatBackground ReadBackgroundFromFile(string id)
        {
            ChatBackground chatBackground = new ChatBackground();
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {

                        if (!store.DirectoryExists(CHAT_BACKGROUND_DIRECTORY))
                            return null;

                        var fileName = CHAT_BACKGROUND_DIRECTORY + "\\" + id;

                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                chatBackground.Read(reader);
                                reader.Close();
                            }

                            file.Close();
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            return chatBackground;
        }

        void GetNewBackground(string msisdn, string id)
        {
            SetDefaultBackground(msisdn);
            ChatBgMap[msisdn] = new BackgroundImage()
            {
                BackgroundId = id,
                Timestamp = TimeUtils.getCurrentTimeStamp().ToString()
            };

            SaveMapToFile();

            //make http request
        }

        void postUpdateInfo_Callback(JObject obj)
        {
            if (UpdateChatBackground != null)
                UpdateChatBackground(null, new ChatBackgroundEventArgs());
        }

        /// <summary>
        /// Read default Bg IDs from file.
        /// This has be done if server deletes a defulat category it is handled by client
        /// </summary>
        void ReadDefaultIdsFromFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = String.Empty;

                        if (!store.DirectoryExists(CHAT_BACKGROUND_DIRECTORY))
                            return;

                        fileName = CHAT_BACKGROUND_DIRECTORY + "\\" + bgIdsListFileName;

                        if (!store.FileExists(fileName))
                            return; 
                        
                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                int count = reader.ReadInt32();

                                for (int i = 0; i < count; i++)
                                    BackgroundIdList.Add(reader.ReadString());

                                reader.Close();

                                file.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chat Bg Helper :: Read Chat Bg Ids from File, Exception : " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Update or fresh install, load the files onto the client
        /// </summary>
        public void LoadDefaultIdsToFile()
        {
            List<ChatBackground> chatBgs = new List<ChatBackground>();

            //BitmapImage bmp = new BitmapImage(new Uri("/Assets/pattern.png", UriKind.Relative)) { CreateOptions = BitmapCreateOptions.None };
            //var bytes = UI_Utils.Instance.ConvertToBytes(bmp);
            var imgstr = "/Assets/pattern.png";// Convert.ToBase64String(bytes);

            chatBgs.Add(new ChatBackground()
            {
                ID = "0",
                Background = "#ff4fb17b",
                SentBubbleBackground = "#ffc6ffe0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 10,
                Thumbnail = null,
                Pattern = imgstr
            });


            chatBgs.Add(new ChatBackground()
            {
                ID = "1",
                Background = "#ffc8544f",
                SentBubbleBackground = "#ffffdbd9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 11,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "2",
                Background = "#ff515b61",
                SentBubbleBackground = "#ffdbf2ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 2,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "3",
                Background = "#ff7546af",
                SentBubbleBackground = "#fff4e0ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 3,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "4",
                Background = "#ffe05f03",
                SentBubbleBackground = "#ffffeac6",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 4,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "5",
                Background = "#ff0c9e91",
                SentBubbleBackground = "#ffc6fffa",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 5,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "6",
                Background = "#ffe7ad00",
                SentBubbleBackground = "#fffcffc6",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 6,
                Thumbnail = null,
                Pattern = imgstr
            });

            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = String.Empty;

                        if (!store.DirectoryExists(CHAT_BACKGROUND_DIRECTORY))
                            store.CreateDirectory(CHAT_BACKGROUND_DIRECTORY);

                        foreach (var bg in chatBgs)
                        {
                            fileName = CHAT_BACKGROUND_DIRECTORY + "\\" + bg.ID;

                            using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    writer.Seek(0, SeekOrigin.Begin);
                                    bg.Write(writer);
                                    writer.Flush();
                                    writer.Close();
                                }

                                file.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chat Bg Helper :: Load Default Bg To File, Exception : " + ex.StackTrace);
                }
            }

            WriteBackgroundIdsToFile();
        }

        /// <summary>
        /// Write Default Ids to file. Helps us to keep track of all existing ids.
        /// </summary>
        async Task WriteBackgroundIdsToFile()
        {
            List<String> idList = new List<String>();
            idList.Add("0");
            idList.Add("1");
            idList.Add("2");
            idList.Add("3");
            idList.Add("4");
            idList.Add("5");
            idList.Add("6");

            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = CHAT_BACKGROUND_DIRECTORY + "\\" + bgIdsListFileName;

                        if (!store.DirectoryExists(CHAT_BACKGROUND_DIRECTORY))
                            store.CreateDirectory(CHAT_BACKGROUND_DIRECTORY);
                        
                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);

                                writer.Write(idList.Count);

                                foreach (var id in idList)
                                    writer.Write(id);

                                writer.Flush();
                                writer.Close();

                                file.Close();
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chat Bg Helper :: Write Bg Ids To File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void DeleteBackground(string id)
        {
            if (!BackgroundIdList.Contains(id))
                return;

            BackgroundIdList.Remove(id);

            WriteBackgroundIdsToFile();
        }

        int CompareBackground(ChatBackground x, ChatBackground y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (y == null)
                    return 1;
                else
                {
                    if (x.Position < y.Position)
                        return -1;
                    else if (x.Position > y.Position)
                        return 1;
                    else
                        return 0;
                }
            }
        }

        public event EventHandler<ChatBackgroundEventArgs> UpdateChatBackground;
    }

    public class BackgroundImage
    {
        public string BackgroundId;
        public string BackgroundImageBase64;
        public string Timestamp;

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(BackgroundId);

                if (BackgroundImageBase64 == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(BackgroundImageBase64);

                if (Timestamp == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Timestamp);
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

                count = reader.ReadInt32();
                BackgroundImageBase64 = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (BackgroundImageBase64 == "*@N@*")
                    BackgroundImageBase64 = null;

                count = reader.ReadInt32();
                Timestamp = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (Timestamp == "*@N@*")
                    Timestamp = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BackgroundImage :: Read : Read, Exception : " + ex.StackTrace);
            }
        }
    }

    public class ChatBackgroundEventArgs : EventArgs
    {
        public string msisdn;
    }
}
