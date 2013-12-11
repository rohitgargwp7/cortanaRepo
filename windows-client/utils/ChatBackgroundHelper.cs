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

        private const string bgIdsListFileName = "chatBgList";
        private const string bgIdMapFileName = "chatBgMap";

        List<String> BackgroundIdList;

        public List<ChatBackground> BackgroundList;
        public ObservableCollection<ChatBackground> BackgroundOC;

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
            ChatBgMap = new Dictionary<string, BackgroundImage>();
            BackgroundIdList = new List<string>();
            BackgroundList = new List<ChatBackground>();
            BackgroundOC = new ObservableCollection<ChatBackground>();
        }

        public void Instantiate()
        {
            BackgroundIdList.Clear();

            PopulateBackgrounds();
            PopulateFromFile();

            BackgroundList.Sort(CompareBackground);
        }

        public bool UpdateChatBgMap(string id, string bgId, long ts)
        {
            if (!BackgroundIDExists(bgId))
                return false;

            BackgroundImage bg = ChatBgMap.ContainsKey(id) ? ChatBgMap[id] : new BackgroundImage();

            var newTs = ts;
            var oldTs = bg.Timestamp;

            if (oldTs < newTs)
            {
                bg.BackgroundId = bgId;
                bg.Timestamp = ts;

                ChatBgMap[id] = bg;

                SaveMapToFile();

                return true;
            }

            return false;
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

                if (chatBg != null)
                    BackgroundList.Add(chatBg);
            }
        }

        Random random = new Random();

        public bool BackgroundIDExists(string id)
        {
            return BackgroundList.Where(b => b.ID == id).Count() > 0;
        }

        public void UpdateChatBackgroundMap(string msisdn, string bgId, string image)
        {
            ChatBgMap[msisdn] = new BackgroundImage()
            {
                BackgroundId = bgId,
                Timestamp = TimeUtils.getCurrentTimeStamp()
            };

            SaveMapToFile();
        }

        int LastIndex = 0;

        public void SetSelectedChatBackgorund(string msisdn)
        {
            BackgroundImage bgObj;

            if (App.ViewModel.SelectedBackground == null)
            {
                LastIndex = 10;

                BackgroundOC.Clear();
                var list = BackgroundList.Take(LastIndex);

                foreach (var bg in list)
                    BackgroundOC.Add(bg);
            }

            if (ChatBgMap.TryGetValue(msisdn, out bgObj))
            {
                var list = BackgroundList.Where(b => b.ID == bgObj.BackgroundId);
                ChatBackground bg = list.Count() == 0 ? null : list.First();

                if (bg != null)
                {
                    App.ViewModel.SelectedBackground = bg;
                    return;
                }
                else
                {
                    bg = ReadBackgroundFromFile(bgObj.BackgroundId);

                    if (bg != null)
                    {
                        App.ViewModel.SelectedBackground = bg;
                        return;
                    }
                }
            }

            ChatBgMap[msisdn] = new BackgroundImage()
            {
                BackgroundId = "0",
                Timestamp = TimeUtils.getCurrentTimeStamp()
            };

            SaveMapToFile();
        }

        /// <summary>
        /// Apply a random background as default if required
        /// </summary>
        /// <param name="msisdn"></param>
        /// <returns></returns>
        String SetDefaultBackground(string msisdn)
        {
            int index = random.Next(0, BackgroundIdList.Count);
            var id = BackgroundIdList[index];

            App.ViewModel.SelectedBackground = BackgroundList.Where(b => b.ID == id).First();
            return id;
        }

        public void LoadBackgroundOCFromList()
        {
            if (LastIndex >= BackgroundList.Count)
                return;

            for (int i = LastIndex; i < LastIndex + 10 && i < BackgroundList.Count; i++)
                BackgroundOC.Add(BackgroundList[i]);

            LastIndex += 10;
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
            var imgstr = "iVBORw0KGgoAAAANSUhEUgAAAPoAAAC8CAYAAABR7bNMAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAB3RJTUUH3QkLDREs0bycgQAAIABJREFUeNrtnXmUVNW97397n1PV1dVDNXNjCw3KcMUBAUPUTBJAFLPeSp6JLo1I0Ks3RhOTFe/DMdeoT+2XuGLWdUhwiWDUiEgUYhi7bYIig2DbA03T3dDQ1VMNXeOp4dQ5Z+/3B8VaXO5A7V3NKTr9+6zV//SqU3v87uHU/u4fgACdnZ1faWlp2QA2wzl/8umnn15md7qhUGj1K6+8cgMAUJvLe3VjY+O/Pfjgg7PtTLejo2Pi4cOHd0nmefHu3bsfe/DBB2dIPDt/69atb3z00UcuO8sbDoef2rJly10AoMAwoqOj490PP/zw2yLPqCIfTiQSJJVKOQtRuL6+PrfdaWYyGR4MBl1ZoTM7tR6Pxx09PT221nUymQTDMJwA4AAAQzTPkUjE2dPTo8qkHQqF3KZp2t2+EAqFirI6sIaL0JPJpBIKhYT6BgXkvIUQUqik+QhJc8SAQkcQFDqCICh0BEFQ6AiCoNARBEGhIwhSEKFzzjnk+TPIya8QhzHGCpFugdLiAMAty+LnQV6QfwBUzvmEHD9rnDhxYmwgECh+7LHHJjzzzDMcAER+6CUAECKEZGQGpPHjx4/inI+TWIVwANAIIUm7KpUQckpgbomBMQMAxYqiOKuqqlyccwUARA/OkOz3mDb/Fs81TZM5eGIBABw6dEj6xMy2bdtgyZIlUm0lM3gORb3m8z2MMaF6VgcHB3+ayWSUHCqEmaY5zjCM0ltvvfXnPp+PM8ZEcunq6+tbDQDNEuUKzJw58zqfz1chmCZhjKU1Tfs7ANTb2eMHBwdVxth3I5GIItKYjDFz1KhR0yzLmnrttdde5/V6J+m6rgp2XtrX19eZTCb3AYBdx82Y0+ks/853vjPrlVdeUTRNyynPlmVZkUhkBqW07Oabb55z++2364LtawUCAd/1118fkBCaaZomkxkcOOfFkUhkSiaTkaoswzCihJA+yYlEGT9+/LSPP/7YN27cOMhlsavW1NR80tPTU0wI4blUqmVZH7hcLqdlCQ/camtrKwe545V/qq2t7dq8ebPomWRiGAb3+/0xOHnM0bYzll1dXSSTyUxrb28vo5SKzOqEc27G4/EjiqKU7ty5cy4XXH9TSpW2trbieDx+0MYymyUlJdOnT59+dSgUinHOcx3dSEdHh8kYSzHGntF1PVeRAqXUwTkvSyQSGwHgGdEMp9PpA36/PyO4Mj3FxFgs9mQymRQ98ssppaMymUw7ANwntQxX1aKKiop7VFX9biqV+sfYWxRyjzkwMPD6U0899V0Q9ATs37//vKm/lStX5vzZxsbGiQcOHNgrWt5sOy3auHHjr0aNGnXJaduHc/kHAODevn37stdee+2FPPoVufTSS6nksyUAMEbwr/zZZ5+9aevWrX8GgFEybdrY2Pjek08+eb1IPZ/3b90LeN5bmvnz5583eampqbEtLYfDoSxatOj0dyPn8g8459b06dOZ0+nkefQrfujQISb5bAIABgX/Yg8++OBgVVVVBuRfbJMpU6YQkXrGn9eQIV19Cb5DyRdqWRaBYWaI0TSN5ltPos+j0BFkBIBCRxAUOoIgKHQEQVDoCIKg0BEEQaEjCIJCRxAEhY4giITQ/X7/sCtcMpmfM1XSUguxWGxI8i9zzl/ATPKfCAaDut1tdGYZ7fI2FNJDwTm3wObTfDmbF8aPHw+RSGRqKpX6Pz6fL0JsPoRumqZ/3rx5vweBi/bdbjeYplmZTqdvSiaTEznnOZ9pppSmdV1frCjK30XzWl5eDqlUysE5/6mmaS7RTkUIcSSTyd2EkFqJwYlzzr8NJw0XIglzxtiYxsZG2aglKqXUSSmlgvkFwzDKVFWtAICeHFyUZz5PKaWqRD1Bc3Mzueyyy0Zn68ouDMuyqgYHB9U8xK5QSsk5EToAQCgUmphMJm9obm7+f2PGjKH5zB6iVFRUPAAAqwBAE3kukUhM6+vrW/rpp59+NmbMGEtQdG81NTV1yuR3YGDAYZrmvZ999tkfy8rKRJymhBBiDAwMUDgZKkjYD9zS0jL52LFj1aKRTxhj6uDg4JZsuqL21s7u7u7Snp6ehPAIoarV27dvv8WyrGfg5IUZuWJFo9GBvr6+AZk2uuyyy8Z0dXXd6/V6J2ualrRj7uKcs+Li4imZTMYBktF/dF0Pa5pmnDOhx2IxM5FINC1btuxVu5c7+/fvvw8k7JO6rhdHIpHwPffcszo7SIjMWAwAyKxZs6zW1lahdCORCJimmVm+fPnrAKCDhOd5ypQp5Pjx48J1tXPnzq27d+8uNQxDKE3LsviRI0cSIOdh79qyZYu/vb1dZrtTEgwGLzVNU/SdkeHz+T7v7u7ukOxWpV6vd7rP52vUNO0LSqnjnE/nhmFcdNFFc10u1zUg54MHr9f7h46Ojt7s83zIhU4IAUVRCvICz+FwSC1zOOfc6XSybOc1QPzSCxAV+Wl1xbIzlNTeV0bka9euheXLlw/Y2TbZK5EsAIgBAFx++eXQ3Cx0kRAnhBgiM2o2TQ4AYQAIr1y5UsqSG41G4/F4fPfdd9/9uV31FY/Hk11dXXNA4mV4ttyfAwDce++9sGrVqty2ojByICMh3eXLl9tfwDMEKijyIUlT1ndPKSWKotgdzNIpu+09vdy5inykCR1BRiwodARBoSMIgkJHEASFjiAICh1BEBQ6giAodARBUOgIgkgKnTGmy0afLCRZR4mVx/P5PGeNtE6FoZrPP9TBwcFtuq6ftWUIITyZTJYdPXr0hGgihBBgjF1MCFkEAKNBwrXT1NSkyhayqKho7JtvvvnVZcuWxQQGNwonTTAdhBDhs+qUUkIIqdi5c+fV3/rWt1JQuCO4ojDJlZ4CAH0A4CWEMInBgd15551pm8uaJoQw0ckr37DJlFIdTnov8oqQ2NDQAHPmzMlN6LfddtsvotGo+2wfjMfj5q233nrFnDlzvieTKUJIqKamJjo4OOjOGiCEiMViv5MZIDRN6+rp6elOJpMPrFu3LpNrKBvOuVVeXj6js7PzfgDYJ5puMBg0TdPc3d3d/UB/fz+z09IrOxGrqlo0evToS30+335CiEOw8zp7enr2btmyZS0AiJpqrMrKyqsbGhq+FPWj51PeQ4cOFUUiEW9bW5sm2JeBc35JOBz+TTqdFj0nz9Pp9BhKafW+ffumTpkyJZMNK5UrDk3Tdmqa9vKcOXOCOc/o27dvP5xrBm+88cYiy7JkZ9ZIIBB4PxgMEsG7CQAAYGBgQGr5bVnWMcMwVnLOqWEYOS8r77zzznRDQ8PG/v7+UpnCmqapm6Z5F+ecGIYB5zuJRILNnj27yuFwvLto0aIf79q1yzh+/DgREDqYpsksy5IprBIMBg+sXr36Mb/fr9t1pwmlFHp7e3koFApIPD5mx44dyV//+tePi17Ckv04MQyDq2rucgqFQuYDDzxw8ejRo/83Y2wsAOQudBC45aKoqIjLxGM+zVJo2tl5GxoaYMaMGRzELjM4vSMwmZt0tm/fDtdffz2ApD21UDQ2NqYNw+AAkP7mN78p3VZ33HEHvPXWW6IDMrv22mu9999/f3q41JdhGKy1tbXRzlXXo48+mvnggw8ysVhMaLZU7chdoUIf57p/GWqyIh+xiIr8FGVlZcOtqIWI5Mpl9IQ/ryHICACFjiAodARBUOgIgqDQEQRBoSMIgkJHEASFjiAICh1BEEGhn3E+fFh6EGWtk5ZlseHauDIRZuys29Ng+aYrmochyjMvUL1xy7KEfB85HYE9deROVVUXIcTzyCOPXPyjH/0I0um0bWdbVVWlnHNL0OkDAEA0TYsTQoTDFBFCHBMnTqz64osvpkyaNAkYY6LPWwCg2OnP5pwTRVH8DocjWVFRYUvChBBIpVLU5XKVAkCRoAAsABhNKVVk0s26AqmoIzL7rJJMJsemUim3aZo5Hy1ljDHDMKocDke5z+ebIlNlhmEkCCEysci5qqpFEydOnNra2qobhnFWDX/961/vUgU6kHrs2LExg4OD42644YbX4/E4sbMDK4rytWg0us/pdIqaLZy6ru8AgF9JJOu/4IIL/rm4uPiWcDgs0Q/JhZZleWXcetJLNErL/H7/u7FYbA0A2GYQcblcRfF4fKmu6/MsyzJztZtyzrnL5apMpVLJZDIp3KEikYjHsqw5AFAvke1p4XD4oVQqNZVzLuTWSiaTxcXFxRXRaPTfZSaQVCr1GQA8JbPILCsrq3I6nf83kUjE4ez3HCic8++pOTYGEEJMAPgAAP6W/bcJ9l2mwA8dOlT/ta997UcAcEIiXXrZZZc5WlpahCyUV1xxxV0AMAoAHBLLNPfmzZv/unTp0nvBPtee+dxzz83xeDwPcM7ft1PoAJB+9dVXD3V2duqJRELI9GdZFu/s7AxlMhnhevL7/ZMzmcy/SQr98o8//rh3xYoVL1uW1Q25R9o9ZWaRvaSDAAAfO3ZsSTAYFA0zrYbD4Y4PP/zw39euXXsoxzyrQkv3LBkoANllKJXcz1ktLS2ySYelRibOXV9++SUDgBDYaFd9+OGHA++//35Kxk6cz14zO4M3Z/+kWbRoEdTW1ub8+VQqxXRdN0DOSWY5HA7rBz/4Qejdd98N2d2ng8Gg7KPq4sWL2dq1a09tfc5azmHz1j27Px4u1zFBY2MjzW5t7K5jyhiz9d62obQhi4j8VNp5pk8Mw1BgmGGaplCh8ec1BBkBoNARBIWOIAgKHUEQFDqCICh0BEFQ6AiCoNARBEGhIwgiKPShPGVls5NLOt1889nT02Nmv8McDnWF/GOT81n3SCRCPR5PJQDMAvmjqJ8TQiIyDxJCiiWeAc65AwBUQkhK4lkPAJRIlre4ubm5+NVXX5384x//WCSaKgEAAwAiMlFcT2adUNnw1pxzE/I04YhE+cw3MqmiKIls9FaZUTEJALZHU80TnXMunOecbaoej8fp8/mu6erquiUYDB4S8Q8zxsyxY8fOP3z48B/hpPtNuFE0TdsIEoaWnp6ey1Kp1FUA8Jros4FA4MrW1tYFuq67RdLODhJqOp2OVVRU/Li2ttYS8bKrqurSdX0nAHwo0REyyWQymU6nhetYVVVFUZTZn3/++cqpU6eK3G1AAIBkMpljmqbtvOSSS4KCdXVhb2/v3aFQSOUCyxhCCNd1fZymaZNqa2ufnjBhAs+1njnnZnd39xzLsrzpdFrYy84YqyCEXAP2XsRiAcBFJSUlY6PR6LkROgAokUhkdGtra+Sll15686KLLnLn+uDu3bu1999//59CodAFcNLyKeyAO3DgwKvZmU6IWCw2OZFIXCcj9FQq1e3z+fYkk0mnaDhfzjn4/f6to0ePdul67hNzJpPRp0yZ8r8IIV+TFHp3V1fXn1KplKj9EYLBYNQwjJpAIMD7+/tzvm+Ac85KS0tHp1KpmyKRiB8AdgkmXXX8+PHvaJq2nnNuCAiOZDKZUDAYbBwzZoy7t7eX5DpQWJZlAoCm67rlcDiEK5kQ4vjkk0+uDIfDRTYKnRcXF5c0NTV19Pf3x86V0IExZqqq6mtoaDje0NAglMNLLrmk58iRI1IW17q6Oli4cKGUXdQ0TUvG59ze3g7V1dVdANBl99rs0KFDFwSDwdkyS29CiAYAO0WXmNnPxgHgGQAoFpypzFWrVk0uKyu7P5VKeWQ6cFdXV/uyZcteBBtt0IlE4rtNTU1XOBwOGfdaeMOGDe/19PQ47MovIQRM0+Td3d2ZcDjsP2dCz0IlBUcZY1Ibm4ULF+ZdQaLMmDGjYC9OTNMUWsL+d+UUKfcZnxU2s99zzz2puro6Q9d1qaWsoijK5s2bydKlS22rZ7fbDQ6HAwSvXzv9IpajhXzBJuLdx5/XkKGCit6p919sHex+wyWVXgFfxP0HRLz7KHQEGQmjMFYBgqDQEQRBoSMIgkJHEASFjiAICh1BEBQ6giDnQuhpxpidYZgQBBkiVM75L3P4HAcA14QJE2aapmn7sb9oNAoej0fqWc65nrVeFoSDBw/CvHnzZPKdwe551jr6D6fUduzYAYsXLxb5CmJZlrlr165kofJsm9BffvllbWBg4GxONF5cXMzGjRvH4/G47Zn0eDwQi8U+0jSNCp7ftjRNu9A0zQvb29v/Vl5efirMbq6NYlBKgXOuiq5kOOem3+9/+8orr1wnWl6n0+koLy//yZ49exa43W5q4wUUxDRN71VXXXUL2BcYEgCAUkodolFnsxbXinQ6vXDXrl0NixcvPiaYbqa6uvrG2trapZWVlRHGmFAG0ul0y9SpUx8Vqatsnsf6/f6f9vX1fY8QIhoIkzkcjjGWZcUZY0au/VL1er1rent7/8folydOnOAvvPBCiaIo329sbJxciNH7kUce+fnhw4fHSIyGhDFGOOdMUXI3KXm93vTvfve7HzQ3N6t79uzZEYvF4pTSnBNnjIHX603IlNU0TWNwcHD9ihUrnr3pppuoREx4aXRdN+Gk2clOobfu2rXrLdM0ZdJ0hUKhOW+99VYaAESFvuW5557r1DRtlMPhED6oH4vFTABwStTV4Icffvji3r1736GUCqVbX18ff+ONN1Y3NDSsq6ur2z1hwoSc7MRqTU1NTmbpq666SmlrazPsjPV9Oi+//PJRiYbMB7Z06dKvtLe3j9V1vam+vj4ANr28ZIyBw+GI9vb2Hlu1apXtdV1ZWQkDAwN2Jhnr7+//a9YjLjyQc84V0zSF2ia7hLYA4Eie7524RLocTkbplbJeezyeRGlpqe+vf/1rZ85Ld8EKLeiWDOy9zQMAgFNKoby8/NSoy+xKuJAOKTtFftqe1QQAuOOOO+Ctt96ys35t7VdD0a6EEBBZnQLgz2tIgTmz49sh8n8ERCddFDqCjABQ6AiCQkcQBIWOIAgKHUEQFDqCICh0BEFQ6AiCnLdCL8TptpMJD5MorsOljP9N2+adB8nneb53yhes0mxqM6FILVnni+wZPl5cXFz80EMPeX7zm99kwD5fOwOAVDayhi1kHUoAAMWiUVzzTTeRSBC3262CvfcGmADgpJQqRUVFjmwHdgp+hwEAXOKIKAcAWllZWcI5dwFAkeCzAIW5Y8EEgKRoTD87hM4ty8oYhmFIqZxzffLkyd/+6le/OiUajZqy4ZlEVyyWZQUjkchHANBmZytqmuYEgHsB4Pd2put2uz3RaPS6RCLh4DZNF5xzy+PxjHM4HBdXVlYagUCA9fb25hyTjFJaNDAwsFvTtBMSKz5OKS1fsGDBLclkcpau66qAyEuygrPb+69YlnWiubn5zyBpbDmXQtePHj3a0NTUNCiVkKqqra2tR1944YWG8vJyWzogIYTE43Hr+PHjqs0NCZ2dnQ7Lsu6Dk1FckzYmXd3W1rayq6vrcCaTSdoxW3HOmdvtLuecV+q6Dl6vtxQAcnJdMMasSZMmffPzzz8fF41GXwJxyycxTTNdV1fXsm7dui9VVc2prcPhcOq22277SX9/f+enn366p6ysTLGrgQghNB6PJ/bs2VN6Xgn9NEvfIQA4tHLlSlJTUyMkVsuyHIqiNG7cuHEdFIAFCxZAfX092NiYnFKaLsQ7iWPHjnVGo9H777vvPtu2DZzzitra2l/29PTsWrFixQ7BZ3++bds27vf7ZQYlQilN+3y+Pe+9994WkQdramoWeDyexoceemhDofbol19+OTQ3N5/7pW2ue78zKkim8/LswOIsRIXaKfJCoygKdbvdJTYnW8I5dxBCZOKFOymlPA8LJ1EURXjVZpqmYlmWUsi2skPkOQsdQZDhDQodQVDoCIKg0BEEQaEjCIJCRxAEhY4gCAodQRAUOoIggkIfCm8E59zKGmIyeXwHthiCSJDTsUFCCKRSKepyuSYCwEwQNx4kYrFY9YwZM+DYsWMLp06dKmpTNQHgS0KIkDkknyisQwHnnAGA8HlzSmmG2hj76swIn/v37yfz588XHlUJIZQUMsSMvW1bsGg6nHNuWZY15EIHAHC5XMTr9U7o7e1dlEgkLJFCMsb0ioqKKZFIRG1qajKOHz/Oc41qyjnnHo/n4t27d78BALUihfN4PJBIJC6KxWLTJ06cuM3OxlAUhVBKqzZv3vzIpEmTrFwvRqCUmpqmzQsEAp125TXrn6eJRGJae3t71dy5c2WMAWbqJIUIUa2IBMAcwjqbWICyxtra2sqqq6vHM8Ym5qpDESOAVV9f37l79+51wWBQaOBubGzUXn/99eldXV2HN2zYsP3CCy+0chW6YRjsiSee+Nnhw4dnigodACCTycyIRqO3AMBOANDt7AiMMR4Ohx1FRUWqgNBdoVDoy4MHD36SXfXYtV9RdF2/vK+vbyEAyAg94vV6t3i9Xp/NHV9PJpMnAoGA7QNMe3v7su7u7gqbB5hMJpNxaZq2cNu2bbMcDkdOocBFbKoAADEAaJTJ4OWXXz7Q2dnZu3fv3i9En924ceOJ1atXSzVkJpMx4/G4BQAuO4Vumia3LKvvhz/84fOS7yXI888/Dw8//LBtncgwDBYMBoXdXNn+oQPA3gIsa2PBYHBTf3+/7XdJhcPhht7e3lKbhU4GBwf3FxUVqYlEQhnSGX0oGs0wDMoYO2VTFe34eVkJKaUFuauOEEJB/pcNbqfIz6gr4dXLUPcXgQHGBAAvgL13Duzfvx/mz5+/A4YJ+POaDXrHKjh326PTsfPOgfnz5w+rukKhI8gIAIWOICh0BEFQ6AiCoNARBEGhIwiCQkcQBIWOIAgKHUEQQaEPhQ+cEJKBwoVM5iBurQUAsLjNJvghSM7Cbo2cSc5n3TnnnnQ6vSwajS7hnFuCIjfS6fS3CSFPwskwxnbCnE5nxerVq69asWJFVGAVkwaA6S6XSzdN+4xRp+oaACYDgENwcLQA4BJVVWUjqRLGWN6F3bZtGyxZskSoH9KTCKXT29sLVVVV8rMcpSZjzNZIqkNp+BGpZxGbaonf77+wpqZmu8/nO+B0Oh0CheMOh+MPLS0tXrD57LeiKFYmk7lQ1/V/Wb9+PcnVLgoA4HK5PI2NjXXt7e22WiAHBgYmHjly5M6+vr5qSqlI2rykpGRsX1+fmU6nhcMPM8ZS48ePv7GpqekzwbDWnFJanEgkjobD4WeXLFki6lDc29bWNnFwcFAoz1VVVTAwMHCxrut/DoVCTCTWOKWUhUKh0WVlZYsPHDhwf9buKTQoxuPxlm984xs/BYHLRbID+Vi/339/X1/fdwkhacF6LolGowc1TfvVkiVLeoZ0Rj+VRwDgPp+va8OGDXtA3FHGAQCqq6vZiRMn7JvOGXMAQPt99933q/r6+mh3d3fO+dZ1HTRN05PJZMpOoTPGOg3DeFrXdUVkluvu7jZ/8YtfXLlp06af6Lou+v7FbG5urv/oo49ucDgcQgEL4/G4ddNNN1VZlvX9WCxWKVHkTzRNU5LJpPCAumPHju5QKHRbR0dHkaIoXERw8XicAwC43W4ic6FPMBg0JbdKoR07dry8d+/e9xVFyXnmiUQi5u233z49EAjclk6ny4Z86X46TqeT57MXtFPkpy2VDACILFiwIC77PXZZINvb26GqqsqEk95/YR5//PGooihMpOOetpxMA0CHTLqvv/56Ytu2bfFEIkEl28cQrefTnj1aoK0vIYQI7ZKyeWYAEMz+CbFmzRrywQcf6KZpCq2MR9Jb97y2DHZZIGfMmGF7OYdqz0gpFf6ufKym58H1dMKvQoYizzLfgT+vIcgIAIWOICh0BEFQ6AiCoNARBEGhIwiCQkcQBIWOIMg5Ebp0EITTDxVgRFQEsR+Vc+7O4XMMANyUUlVGqKeC+AEAFzEeFJIhcBlxkDildnoE2HzyYJqm3fXMC1TPwxq7yq+mUqlHLcs6W0pcUZRyxlhVPB7fLpOQYRiXDgwM+AHA1iB82WiQUsdCOee0q6urIpFIqJZlgWAE2WLTNIX9ANkIsK5AIKASQjSpZRql6qxZs0o552mwxy1oAUAppdQhcySUc14UDAZLsv1wOKmeZQ+7S+U5k8nohJC4HRlVn3nmmYO9vb3uXEaeeDyuHT58WCqcr6Zp9+zbt+8AALwNNl6OwBhLmqY5KPn4aMuyXjRN8xLReNScc97a2rpHpuMyxn6QyWRmAcCjEjOlXlJSMrq6uvrnsVgsaYdwOOfM5XKVOZ3OsfF4XGZw+n40Gr2DUqrA8GIM5zwkuUp1pNPpegB4yhahP/vssx+IPjR37lz44gsxy3Emk9HD4bACJ+2ttgg9uyzaDQD7R40a5QyHw6LPBgHgDjh5AYSMYOjkyZON7u5uoYeSyaTZ39/vAIASABAVTseBAweeXb9+/YQcVmpDtWoCTdPMXbt2HQ8EAq0SXzGppqZmx2uvvbYe5G4CKgTWli1b6m+88cafSLQRZPsTGzVqVEk4HE6cc6HLPCQq8lOdwe692GnpGeFw2JB8FiBroZRBVOSn0paJAJsdnCyQi29e0K3q6NGjzf379/fOnz+fDZdMX3DBBRwABgEgJPsdIpNPPuDPa/9ADOOXWhwA4JVXXhlWS/fsbUXDQkModAQZAaDQEQSFjiAICh1BEBQ6giAodARBUOgIgqDQEQRBoSMIkqfQGxoaZB4j5slohTJB7cxMJmMOxwqW9d9nA1nqI6W8I4WhiJbLOWe6rgv5RdRwOLw8k8nkkjphjMVisdiBmTNnemWEPnXq1Es6OjqumzZtWgZyN4kwAJg+e/bsUZzza4bRKsQEgE5CyKBEZyAlJSWT//a3v123dOnShGCZTw2ICthv+ewghPhl+oau65k1a9YYhWgoieiv0mRtuU4AGAPikYUNAJjgcrnK5syZM4FzHoQc/SpqV1fXxbFY7GyRUbmiKE5CyPi+vr4JAPAH0QLquv55KpW6+siRI9f39/dbIlFNS0tLuaqq5bt27Vo6HGYMQghxOp2e/v7+IwDwkvCQbVmHo9Ho7EQisfjTTz/N5OqQ5ZyzsrKy6lQqlbEsKwA2hajVBKvLAAAFmElEQVTmnFtlZWUzg8HgVgBYKzNLzZ49+1sHDx50q6rKbGojNZFI9MVisdolS5b47ewfx48fnxiPx38UCARI1ryUaz3zsrKyimg0OoYx9r2///3v1+XqhVfnzp37NAA44X92SnG321365ptvfj8UClVLDfUdHR/t2LGjMRAIuBljIODhJdFo1JwwYQLU1dWp5/sNNZxzcDqd9Oabb77G5/NdKyP02traL+vr6/2pVOoCwzByDgfs9Xr1F1988e66ujqrra1ts2EYKTuMLh0dHannn3/+X/x+/8UAUAwCYYRP68POUCg0hlJq2dBGrLS0dEI4HP5WJBI5DgC2Cj0UCiVDodCR7u5uRSQCLAAQzjkbHBzcXV5e7nQ6nRRydDiq2eXAWZdMiUSCd3Z2pvbs2SPcEHV1dbBw4UINAA6PlL3YO++843n77be/Ivrc5s2bYenSpRwAerN/Qlx99dWdtbW1RX6/f//HH39s21L4mmuuad+7d2+F5ONqQ0ND3V133fVHsOmugra2tkler/eXyWTSaWe/2L9/P8ybNy8AAO/ama7I3k8xDIPILJ0XLlw40l640GQyqXKJylq6dGm+yasAQCsrK23twLquqyLbsTNxu93qE088Yds7hZkzZ0JJSQlXVdXWvjF//vyC9En8eQ05L8bG7NbDzpeH0ne9DUdQ6AiCQkcQBIWOIAgKHUEQFDqCICh0BEFQ6AiCoNARBJETeooxZliWVTC7qOhBs6E0wMh8Fy+MA8eyLIu98847pp1lRc5vVM75A3D2g/EcAFyVlZVfmTJlynjO+T8DQJFAOgRO2ic5yNsnPyOECBnhs5bAcQCwCADGgpib61Rkzw2EkD4Jcwhxu93jOOczBesqH9IAUD158mRHKBS6ctSoUTJBFtOEkHab+6HUCbXTQ0wXCJat87wmEJG+JRtmWX3jjTf4wMCA+2wOqWz44ZZ0Ok337dsnZF4wDMOcMWPGvFQqVRQIBNpM00wQsdySxsbGiwBA+MaLSCQy8+jRozevX79+X0VFBRfJ86xZs67p6upS77333tdXrVoVE+28xcXFU997773HCCF2bZG4y+Uqi8fjZOvWrT9TVVV0aiaGYQwCwOMAIFReQoiSjYYqsxxwgkRwRY/HA5qmTU2n0/ePHTv2XwXTppRShVIqfQzW4XBc2NHR8bNp06ZpEgPbCQDYSghJC9YzcM6vAoB5AODKtcxqa2vr2p6eHprDSMItywKn0wmMMaHKeffdd1OxWOyOdevWTW1oaHhP13Wv0+kUirPV3t4u5WoyDMPFOY/X1NS8/cADD0SDwaCSa57j8bj12muvleXg1/8vqywajTanUqkHVVW1a0aHeDzOTdOEdDpNFEWRqS8LJKKDRiKR5o6OjrFZ0YrOcr/ftGnThYlEQriNk8lkUSQSuRgAKgBAJGJhuKOjo8Xn80Vk67qzs/PxhoYGt6qqOfcPxhgbN27ceI/HM7e+vt4HAHtE0w2FQrds2rRJ6erq6nM4ckta/e1vf6vZ0QHLysoSDodD13U9/pe//EUqTOyCBQugvl4sUCjnHCilBgDEX3rpJaF0S0tLE4qilEi6srhpmum77747DCOAdDq9KxqNUhC8Kiy7FI0BQBtIXJSRnYBObQlFiMVisT9Fo9F8IuWuPnbsmGKaZs4L1E2bNpktLS0zw+HwHZs2bSqRSdc0zQqfz7fz6aef3rx8+XIjk8mcNXE7PXqEc07yWSqJinwI9oF5uZuGcXRTIQ4ePAhXXHFFJs86yvdmGS44uHAASAIArFy5EmpqaoQSW7t2LSxfvlxqf15dXa2l02kjj/sEAQB0zrm2Zs2anLY8+PMakjfz5s0bVvk9cwAWFTkAwPLly+2edPL6HhQ6gowAUOgIgkJHEASFjiAICh1BkPOD/w/9U8eXKdHT3QAAAABJRU5ErkJggg==";// Convert.ToBase64String(bytes);

            chatBgs.Add(new ChatBackground()
            {
                ID = "0",
                Background = "#ff4fb17b",
                SentBubbleBackground = "#e5ffffff",
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
                ID = "9",
                Background = "#ffffffff",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffefefef",
                BubbleForeground = "#ff000000",
                Foreground = "#ff000000",
                IsTile = true,
                Position = 0,
                IsDefault = true,
                Thumbnail = null,
                Pattern = null
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "1",
                Background = "#ffc8544f",
                SentBubbleBackground = "#e5ffffff",
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
                SentBubbleBackground = "#e5ffffff",
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
                SentBubbleBackground = "#e5ffffff",
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
                SentBubbleBackground = "#e5ffffff",
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
                SentBubbleBackground = "#e5ffffff",
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
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 6,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "7",
                Background = "#ff6EA510",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 7,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "8",
                Background = "#ffD662AA",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 8,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "13",
                Background = "#ff1E648C",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 13,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "14",
                Background = "#ff2AAAFF",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 14,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "15",
                Background = "#ff7F55FF",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 15,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "16",
                Background = "#ffFFFF7F",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ff000000",
                IsTile = true,
                Position = 16,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "17",
                Background = "#ff8E681C",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 17,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "18",
                Background = "#ff3C516E",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 18,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "19",
                Background = "#ff05A2A5",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 19,
                Thumbnail = null,
                Pattern = imgstr
            });

            chatBgs.Add(new ChatBackground()
            {
                ID = "20",
                Background = "#ffA20D08",
                SentBubbleBackground = "#e5ffffff",
                ReceivedBubbleBackground = "#ffffffff",
                BubbleForeground = "#ff000000",
                Foreground = "#ffffffff",
                IsTile = true,
                Position = 20,
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
            idList.Add("7");
            idList.Add("8");
            idList.Add("9");
            idList.Add("13");
            idList.Add("14");
            idList.Add("15");
            idList.Add("16");
            idList.Add("17");
            idList.Add("18");
            idList.Add("19");
            idList.Add("20");

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

        /// <summary>
        /// Delete a background. When implementation is done on server in future, call this function
        /// </summary>
        /// <param name="id"></param>
        public void DeleteBackground(string id)
        {
            if (!BackgroundIdList.Contains(id))
                return;

            BackgroundIdList.Remove(id);

            WriteBackgroundIdsToFile();
        }

        /// <summary>
        /// Clear all data on unlink or delete
        /// </summary>
        public void Clear()
        {
            ChatBgMap.Clear();
            SaveMapToFile();
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

    public class BackgroundImage
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

    public class ChatBackgroundEventArgs : EventArgs
    {
        public string msisdn;
    }
}
