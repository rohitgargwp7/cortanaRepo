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
using System.Collections.ObjectModel;

namespace windows_client.utils
{
    public class ChatBackgroundHelper
    {
        private const string CHAT_BACKGROUND_DIRECTORY = "ChatBackgrounds";
        private const string bgIdMapFileName = "chatBgMap";

        public List<ChatBackground> BackgroundList;
        public Dictionary<String, ChatThemeData> ChatBgMap;

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
            ChatBgMap = new Dictionary<string, ChatThemeData>();
            BackgroundList = new List<ChatBackground>();
        }

        public void Instantiate()
        {
            PopulateBackgrounds();
            PopulateChatBgMapFromFile();

            BackgroundList.Sort(CompareBackground);
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

        void PopulateBackgrounds()
        {
            LoadDefaultBackgrounds();
        }

        Random random = new Random();

        public bool BackgroundIDExists(string bgId)
        {
            return BackgroundList.Where(b => b.ID == bgId).Count() > 0;
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
        /// When user updates a background of one of his chat
        /// </summary>
        /// <param name="msisdn">msisdn/group id</param>
        /// <param name="bgId">chat theme id</param>
        public void UpdateChatBgMap(string msisdn, string bgId)
        {
            ChatBgMap[msisdn] = new ChatThemeData()
            {
                BackgroundId = bgId,
                Timestamp = TimeUtils.getCurrentTimeStamp()
            };

            SaveChatBgMapToFile();
        }

        public void SetSelectedBackgorundFromMap(string msisdn)
        {
            ChatThemeData bgObj;

            if (ChatBgMap.TryGetValue(msisdn, out bgObj))
            {
                var list = BackgroundList.Where(b => b.ID == bgObj.BackgroundId);
                ChatBackground bg = list.Count() == 0 ? null : list.First();

                if (bg != null)
                {
                    App.ViewModel.SelectedBackground = bg;
                    return;
                }
            }

            ChatBgMap[msisdn] = new ChatThemeData()
            {
                BackgroundId = "0",
                Timestamp = TimeUtils.getCurrentTimeStamp()
            };

            App.ViewModel.SelectedBackground = BackgroundList.Where(b => b.IsDefault == true).First();

            SaveChatBgMapToFile();
        }

        /// <summary>
        /// Apply a random background as default if required
        /// </summary>
        /// <param name="msisdn"></param>
        /// <returns></returns>
        public String SetDefaultBackground(string msisdn)
        {
            ChatThemeData bgObj;

            if (ChatBgMap.TryGetValue(msisdn, out bgObj))
            {
                var list = BackgroundList.Where(b => b.ID == bgObj.BackgroundId);
                ChatBackground bg = list.Count() == 0 ? null : list.First();

                if (bg != null && bg.ID != "0")
                {
                    App.ViewModel.SelectedBackground = bg;
                    return bg.ID;
                }
            }
            int index = random.Next(1, BackgroundList.Count);

            App.ViewModel.SelectedBackground = BackgroundList[index];
            return App.ViewModel.SelectedBackground.ID;
        }

        /// <summary>
        /// Update or fresh install, load the files onto the client
        /// </summary>
        void LoadDefaultBackgrounds()
        {
            BackgroundList.Add(new ChatBackground()
            {
                ID = "0",
                Background = "#ffffffff",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffefefef",
                BubbleForeground = "#ff000000",
                Foreground = "#ff000000",
                IsTile = true,
                Position = 0,
                IsDefault = true,
                Thumbnail = null,
                ImagePath = String.Empty
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "1",
                Background = "#ff03b1c5",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 1,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbBeach.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "2",
                Background = "#ffeb8205",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 2,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbBikers.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "3",
                Background = "#ff8455be",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 3,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbCelebration.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "4",
                Background = "#ff0e8ee0",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 4,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbChatty.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "5",
                Background = "#fff47e00",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 5,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbCheers.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "6",
                Background = "#ff4a738a",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 6,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbCreepy.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "7",
                Background = "#ffde557c",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 7,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbFloral.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "8",
                Background = "#ff27aa27",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 8,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbForest.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "9",
                Background = "#ffa8b900",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 9,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbGirly.png"
            });


            BackgroundList.Add(new ChatBackground()
            {
                ID = "10",
                Background = "#ffff5655",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 10,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbKisses.png"
            });


            BackgroundList.Add(new ChatBackground()
            {
                ID = "11",
                Background = "#ffe94e4e",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 11,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbLove.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "12",
                Background = "#fff8a600",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 12,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbPets.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "13",
                Background = "#fff8b100",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 13,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbSmiley.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "14",
                Background = "#ff5d57af",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 14,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbSpace.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "15",
                Background = "#ff9bb300",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 15,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbSporty.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "16",
                Background = "#ff065eac",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 16,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbStarry.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "17",
                Background = "#ffbb8c2f",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 17,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbStudy.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "18",
                Background = "#ff1a9ecd",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 18,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbTechy.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "19",
                Background = "#ffe23e60",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 19,
                Thumbnail = null,
                ImagePath = "/View/images/chatBackgrounds/cbValentines.png"
            });
        }

        /// <summary>
        /// Clear all data on unlink or delete
        /// </summary>
        public void Clear()
        {
            ChatBgMap.Clear();
            SaveChatBgMapToFile();
        }

        /// <summary>
        /// Sort background on position id
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
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
