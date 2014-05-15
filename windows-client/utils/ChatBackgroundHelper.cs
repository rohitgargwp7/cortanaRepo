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
        public Dictionary<String, WriteableBitmap> ChatBgCache = new Dictionary<string, WriteableBitmap>();

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
            int id = random.Next(3);

            if (id == 0)
                id = 24;
            else if (id == 1)
                id = 25;
            else
                id = 26;

            App.ViewModel.SelectedBackground = BackgroundList.Where(b => b.ID == id.ToString()).First();
            UpdateChatBgMap(msisdn, App.ViewModel.SelectedBackground.ID);

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
                HeaderColor = "#ff2B8DDD",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffefefef",
                BubbleForeground = "#ff000000",
                Foreground = "#ff000000",
                IsTile = true,
                Position = 0,
                IsDefault = true,
                IsLightTheme = true,
                ThumbnailPath = String.Empty,
                ImagePath = String.Empty
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "24",
                Background = "#ff9cbb79",
                HeaderColor = "#ff75a69a",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbSpring.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbSpring.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "25",
                Background = "#ff566761",
                HeaderColor = "#ff4a5957",
                SentBubbleBackground = "#ffffd7ac",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbMountain.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbMountain.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "26",
                Background = "#ff8daac2",
                HeaderColor = "#ff2e5ba0",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbBeach2.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbBeach2.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "27",
                Background = "#ffabcc41",
                HeaderColor = "#ff96b534",
                SentBubbleBackground = "#ffc6e590",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCricket.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbCricket.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "28",
                Background = "#ff4f7370",
                HeaderColor = "#ff3a6063",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbFriends.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbFriends.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "29",
                Background = "#ff506f8a",
                HeaderColor = "#ff49758a",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbRains.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbRains.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "30",
                Background = "#ffa8abb5",
                HeaderColor = "#ff939bb0",
                SentBubbleBackground = "#ffd2f0ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbMusic.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbMusic.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "31",
                Background = "#ff918171",
                HeaderColor = "#ff827465",
                SentBubbleBackground = "#fffce3c5",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbMrRight.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbMrRight.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "32",
                Background = "#fffbb476",
                HeaderColor = "#ffbd915e",
                SentBubbleBackground = "#ffffd7ac",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbHikinCouple.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbHikinCouple.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "33",
                Background = "#ffb87d45",
                HeaderColor = "#ffa16b35",
                SentBubbleBackground = "#fffce3c5",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbExam.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbExam.jpg"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "34",
                Background = "#ff235ba6",
                HeaderColor = "#ff1f5295",
                SentBubbleBackground = "#ffa8d3ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbAnxiety.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbAnxiety.jpg"
            });
            
            BackgroundList.Add(new ChatBackground()
            {
                ID = "20",
                Background = "#ff8D0000",
                HeaderColor = "#ff7d0101",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 20,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbILoveU.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbILoveU.png"
            });


            BackgroundList.Add(new ChatBackground()
            {
                ID = "21",
                Background = "#ff132332",
                HeaderColor = "#ff263440",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = false,
                Position = 21,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbStarNight.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbStarNight.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "22",
                Background = "#ff244b70",
                HeaderColor = "#ff182936",
                SentBubbleBackground = "#ffb2e5ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = false,
                Position = 22,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbNight.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbNight.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "23",
                Background = "#ff707461",
                HeaderColor = "#ff214549",
                SentBubbleBackground = "#ffa2e5e2",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = false,
                IsLightTheme = true,
                Position = 23,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbOwl.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbOwl.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "1",
                Background = "#ffe94e4e",
                HeaderColor = "#ffd14646",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 1,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbLove.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbLove.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "2",
                Background = "#ff0e8ee0",
                HeaderColor = "#ff0d80c9",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 2,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbChatty.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbChatty.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "3",
                Background = "#ffFB6391",
                HeaderColor = "#ffe15982",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 3,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbGirly.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbGirly.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "4",
                Background = "#ff065eac",
                HeaderColor = "#ff05549a",
                SentBubbleBackground = "#ffa8d3ff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 4,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbStarry.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbStarry.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "5",
                Background = "#fff47e00",
                HeaderColor = "#ffdb7100",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 5,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCheers.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbCheers.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "6",
                Background = "#ff9BB200",
                HeaderColor = "#ff8ba003",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 6,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbSporty.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbSporty.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "7",
                Background = "#fff8b100",
                HeaderColor = "#ffdf9f00",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 7,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbSmiley.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbSmiley.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "8",
                Background = "#ff4a738a",
                HeaderColor = "#ff42677c",
                SentBubbleBackground = "#ffc2dceb",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 8,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCreepy.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbCreepy.png"
            });

            BackgroundList.Add(new ChatBackground()
             {
                 ID = "9",
                 Background = "#ff8455be",
                 HeaderColor = "#ff774cab",
                 SentBubbleBackground = "#ffe3cdff",
                 ReceivedBubbleBackground = "#ffffffff",
                 BubbleForeground = "#ff000000",
                 Foreground = "#ffffffff",
                 IsTile = true,
                 IsLightTheme = true,
                 Position = 9,
                 ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCelebration.png",
                 ImagePath = "/View/images/chatBackgrounds/Background/cbCelebration.png"
             });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "10",
                Background = "#ffde557c",
                HeaderColor = "#ffc74c6f",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 10,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbFloral.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbFloral.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "11",
                Background = "#ff27aa27",
                HeaderColor = "#ff239923",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 11,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbForest.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbForest.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "12",
                Background = "#ffFF5F78",
                HeaderColor = "#ffe5556c",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 12,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbCupcake.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbCupcake.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "13",
                Background = "#ff1a9ecd",
                HeaderColor = "#ff178eb8",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 13,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbTechy.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbTechy.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "14",
                Background = "#ffff5655",
                HeaderColor = "#ffe54d4c",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 14,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbKisses.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbKisses.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "15",
                Background = "#ff02b1c4",
                HeaderColor = "#ff029fb0",
                SentBubbleBackground = "#ffbafff9",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 15,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbBeach.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbBeach.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "16",
                Background = "#fff8a600",
                HeaderColor = "#ffdf9500",
                SentBubbleBackground = "#fffff8be",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 16,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbPets.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbPets.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "17",
                Background = "#ff95B000",
                HeaderColor = "#ffafca18",
                SentBubbleBackground = "#ffdcffa0",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 17,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbStudy.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbStudy.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "18",
                HeaderColor = "#ffDE3B5A",
                Background = "#ffc73551",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 18,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbValentines.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbValentines.png"
            });

            BackgroundList.Add(new ChatBackground()
            {
                ID = "19",
                HeaderColor = "#ffeb8205",
                Background = "#ffd37504",
                SentBubbleBackground = "#ffffebdd",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                IsLightTheme = true,
                Position = 19,
                ThumbnailPath = "/View/images/chatBackgrounds/Thumbnail/cbBikers.png",
                ImagePath = "/View/images/chatBackgrounds/Background/cbBikers.png"
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
