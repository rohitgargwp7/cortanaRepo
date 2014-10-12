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
using System.Diagnostics;

namespace windows_client.utils
{
    public class ChatBackgroundHelper
    {
        private const string CHAT_BACKGROUND_DIRECTORY = "ChatBackgrounds";
        private const string bgIdMapFileName = "chatBgMap";

        public List<ChatBackground> BackgroundList;
        public Dictionary<String, ChatThemeData> ChatBgMap;
        public LruCache<String, BitmapImage> ChatBgCache = new LruCache<String, BitmapImage>(3, 0);

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

            //BackgroundList.Sort(CompareBackground);
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
                    Debug.WriteLine("Chat Bg Helper :: Read Bg Id Map From File, Exception : " + ex.StackTrace);
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
                    Debug.WriteLine("Chat Bg Helper :: Write Bg Id Map To File, Exception : " + ex.StackTrace);
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
            if (ChatBgMap.ContainsKey(msisdn))
            {
                ChatBgMap[msisdn].BackgroundId = bgId;
                ChatBgMap[msisdn].Timestamp = TimeUtils.getCurrentTimeStamp();
            }
            else
            {
                ChatBgMap[msisdn] = new ChatThemeData()
                {
                    BackgroundId = bgId,
                    Timestamp = TimeUtils.getCurrentTimeStamp()
                };
            }

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

            App.ViewModel.SelectedBackground = BackgroundList.Where(b => b.IsDefault == true).First();
        }

        /// <summary>
        /// Update or fresh install, load the files onto the client
        /// </summary>
        void LoadDefaultBackgrounds()
        {
            if (App.appSettings.Contains(HikeConstants.BLACK_THEME))
            {
                BackgroundList.Add(new ChatBackground()
                {
                    ID = "0",
                    Background = "#ff272727",
                    HeaderAndNotificationColor = "#ff232323",
                    SentBubbleBackground = "#ffb2e5ff",
                    ReceivedBubbleBackground = "#ffefefef",
                    BubbleForeground = "#ff000000",
                    Foreground = "#ffffffff",
                    IsTile = true,
                    Position = 0,
                    IsDefault = true,
                    IsLightTheme = false,
                    ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbDefaultDark.jpg",
                    ImagePath = String.Empty
                });
            }
            else
            {
                BackgroundList.Add(new ChatBackground()
                {
                    ID = "0",
                    Background = "#ffffffff",
                    HeaderAndNotificationColor = "#ff2B8DDD",
                    SentBubbleBackground = "#ffb2e5ff",
                    ReceivedBubbleBackground = "#ffefefef",
                    BubbleForeground = "#ff000000",
                    Foreground = "#ff000000",
                    IsTile = true,
                    Position = 0,
                    IsDefault = true,
                    IsLightTheme = true,
                    ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbDefaultLight.jpg",
                    ImagePath = String.Empty
                });
            }

            BackgroundList.Add(new ChatBackground() //Diwali Theme
            {
                ID = "42",
                Background = "#fff8b100",       
                HeaderAndNotificationColor = "#ffdf9f00", 
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffff8be",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 34,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbdiwali.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbdiwali.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "39",
                Background = "#ff988d7a",
                HeaderAndNotificationColor = "#ffc23514",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 11,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbIDay.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbIDay.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "40",
                Background = "#ff93202a",
                HeaderAndNotificationColor = "#ffb42d3c",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 11,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbLove_2.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbLove_2.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "41",
                Background = "#ff366411",
                HeaderAndNotificationColor = "#ff5c9c00",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 11,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbNature.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbNature.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "29",
                Background = "#ff737373",
                HeaderAndNotificationColor = "#ff4b4b4b",
                SentBubbleBackground = "#ffd9d9d9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 11,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbRains.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbRains.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "36",
                Background = "#ffe2602f",
                HeaderAndNotificationColor = "#ffc44523",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 2,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbIPL.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbIPL.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "37",
                Background = "#ffffc87d",
                HeaderAndNotificationColor = "#ffe59930",
                SentBubbleBackground = "#ffffffcc",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 3,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbGeometric1.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbGeometric1.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "38",
                Background = "#ffd94e49",
                HeaderAndNotificationColor = "#ffd73f4d",
                SentBubbleBackground = "#fff5e0d0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 4,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbBlurredLight.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbBlurredLight.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "20",
                Background = "#ff8D0000",
                HeaderAndNotificationColor = "#ff7d0101",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 5,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbILoveU.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbILoveU.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "28",
                Background = "#ff4f7370",
                HeaderAndNotificationColor = "#ff3a6063",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 6,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbFriends.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbFriends.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "26",
                Background = "#ff8daac2",
                HeaderAndNotificationColor = "#ff2e5ba0",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 7,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbBeach2.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbBeach2.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "22",
                Background = "#ff244b70",
                HeaderAndNotificationColor = "#ff182936",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 8,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbNight.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbNight.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "24",
                Background = "#ff9cbb79",
                HeaderAndNotificationColor = "#ff75a69a",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 9,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbSpring.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbSpring.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "21",
                Background = "#ff132332",
                HeaderAndNotificationColor = "#ff263440",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = false,
                Position = 10,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbStarNight.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbStarNight.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "30",
                Background = "#ffa8abb5",
                HeaderAndNotificationColor = "#ff939bb0",
                SentBubbleBackground = "#ffd2f0ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                Position = 12,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbMusic.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbMusic.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "4",
                Background = "#ff065eac",
                HeaderAndNotificationColor = "#ff05549a",
                SentBubbleBackground = "#ffa8d3ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 13,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbStarry.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbStarry.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "23",
                Background = "#ff224549",
                HeaderAndNotificationColor = "#ff214549",
                SentBubbleBackground = "#ffa2e5e2",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 14,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbOwl.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbOwl.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "15",
                Background = "#ff02b1c4",
                HeaderAndNotificationColor = "#ff029fb0",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 15,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbBeach.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbBeach.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "11",
                Background = "#ff27aa27",
                HeaderAndNotificationColor = "#ff239923",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 16,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbForest.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbForest.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "32",
                Background = "#fffbb476",
                HeaderAndNotificationColor = "#ffbd915e",
                SentBubbleBackground = "#ffffd7ac",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 17,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbHikinCouple.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbHikinCouple.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "25",
                Background = "#ff566761",
                HeaderAndNotificationColor = "#ff4a5957",
                SentBubbleBackground = "#ffffd7ac",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 18,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbMountain.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbMountain.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "1",
                Background = "#ffe94e4e",
                HeaderAndNotificationColor = "#ffd14646",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 19,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbLove.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbLove.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "3",
                Background = "#ffFB6391",
                HeaderAndNotificationColor = "#ffe15982",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbGirly.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbGirly.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "31",
                Background = "#ff918171",
                HeaderAndNotificationColor = "#ff827465",
                SentBubbleBackground = "#fffce3c5",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 21,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbMrRight.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbMrRight.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "7",
                Background = "#fff8b100",
                HeaderAndNotificationColor = "#ffdf9f00",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 22,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbSmiley.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbSmiley.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "2",
                Background = "#ff0e8ee0",
                HeaderAndNotificationColor = "#ff0d80c9",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 23,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbChatty.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbChatty.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "8",
                Background = "#ff4a738a",
                HeaderAndNotificationColor = "#ff42677c",
                SentBubbleBackground = "#ffc2dceb",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 24,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCreepy.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbCreepy.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "18",
                HeaderAndNotificationColor = "#ffDE3B5A",
                Background = "#ffc73551",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 25,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbValentines.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbValentines.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "14",
                Background = "#ffff5655",
                HeaderAndNotificationColor = "#ffe54d4c",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 26,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbKisses.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbKisses.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "17",
                Background = "#ff95B000",
                HeaderAndNotificationColor = "#ffafca18",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 27,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbStudy.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbStudy.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "13",
                Background = "#ff1a9ecd",
                HeaderAndNotificationColor = "#ff178eb8",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 28,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbTechy.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbTechy.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "9",
                Background = "#ff8455be",
                HeaderAndNotificationColor = "#ff774cab",
                SentBubbleBackground = "#ffe3cdff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 29,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCelebration.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbCelebration.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "10",
                Background = "#ffde557c",
                HeaderAndNotificationColor = "#ffc74c6f",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 30,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbFloral.jpg",
                ImagePath = "/View/images/chatBackgrounds/Background/cbFloral.jpg"
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
                Debug.WriteLine("BackgroundImage :: Write : Unable To write, Exception : " + ex.StackTrace);
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
                Debug.WriteLine("BackgroundImage :: Read : Read, Exception : " + ex.StackTrace);
            }
        }
    }
}
