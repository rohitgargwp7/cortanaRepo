using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Collections.Generic;
using windows_client.DbUtils;
using System;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Linq;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using System.Security.Cryptography;
using windows_client.Languages;
using windows_client.Misc;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;

namespace windows_client.utils
{
    public class Utils
    {
        private static long MIN_TIME_BETWEEN_NOTIFICATIONS = 5000; //in msecs
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public static void savedAccountCredentials(JObject obj)
        {
            App.MSISDN = (string)obj["msisdn"];
            AccountUtils.Token = (string)obj["token"];
            appSettings[App.MSISDN_SETTING] = App.MSISDN;
            appSettings[App.UID_SETTING] = (string)obj["uid"];
            appSettings[App.TOKEN_SETTING] = (string)obj["token"];
            appSettings[App.SMS_SETTING] = (int)obj[NetworkManager.SMS_CREDITS];
            appSettings[App.IS_PUSH_ENABLED] = (bool)true;
            appSettings[App.VIBRATE_PREF] = (bool)true;
            appSettings[App.HIKEJINGLE_PREF] = (bool)true;
            appSettings[App.LAST_ANALYTICS_POST_TIME] = (long)TimeUtils.getCurrentTimeStamp();
            appSettings.Save();
        }

        public static bool isGroupConversation(string msisdn)
        {
            if (msisdn == HikeConstants.MY_PROFILE_PIC)
                return false;
            return !msisdn.StartsWith("+");
        }

        public static string ConvertUrlToFileName(string url)
        {
            var restrictedCharaters = new[] { '/', '\\', '*', '"', '|', '<', '>', ':', '?', '.' };
            url = restrictedCharaters.Aggregate(url, (current, restrictedCharater) => current.Replace(restrictedCharater, '_'));

            return url;
        }

        public static int CompareByName<T>(T a, T b)
        {
            string name1 = a.ToString();
            string name2 = b.ToString();
            if (String.IsNullOrEmpty(name1))
            {
                if (String.IsNullOrEmpty(name2))
                {
                    return 0;
                }
                //b is greater
                return -1;
            }
            else
            {
                if (String.IsNullOrEmpty(name2))
                {
                    //a is greater
                    return 1;
                }
            }
            if (name1.StartsWith("+"))
            {
                if (name2.StartsWith("+"))
                {
                    return name1.CompareTo(name2);
                }
                return -1;
            }
            else
            {
                if (name2.StartsWith("+"))
                {
                    return 1;
                }
                return name1.CompareTo(name2);
            }
        }
        /**
       * Requests the server to send an account info packet
       */
        public static void requestAccountInfo()
        {
            Debug.WriteLine("Utils", "Requesting account info");
            JObject requestAccountInfo = new JObject();
            try
            {
                JObject upgradeJobj = new JObject();
                upgradeJobj.Add(HikeConstants.UPGRADE, true);
                requestAccountInfo.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.REQUEST_ACCOUNT_INFO);
                requestAccountInfo.Add(HikeConstants.DATA, upgradeJobj);
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, requestAccountInfo);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Utils" + "Invalid JSON" + e.Message);
            }
        }

        public static string[] splitUserJoinedMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return null;
            char[] delimiters = new char[] { ',' };
            return msg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void TellAFriend()
        {

        }

        /// <summary>
        /// -1 if v1 less v2
        /// 0 if v1 equal v2
        /// 1 is v1 greater v2
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static int compareVersion(string version1, string version2)
        {
            if (String.IsNullOrEmpty(version1) && String.IsNullOrEmpty(version2))
                return 0;
            else if (String.IsNullOrEmpty(version1))
                return -1;
            else if (String.IsNullOrEmpty(version2))
                return 1;

            string[] version1_parts = version1.Split('.');
            string[] version2_parts = version2.Split('.');
            int i;
            int min = version1_parts.Length < version2_parts.Length ? version1_parts.Length : version2_parts.Length;
            for (i = 0; i < min && version1_parts[i] == version2_parts[i]; i++) ;

            int v1, v2;
            if (version1_parts.Length == version2_parts.Length)
            {
                if (i == version2_parts.Length)
                    return 0;
                v1 = Convert.ToInt32(version1_parts[i]);
                v2 = Convert.ToInt32(version2_parts[i]);
            }
            else if (version1_parts.Length > version2_parts.Length)
            {
                v2 = 0;
                v1 = Convert.ToInt32(version1_parts[i]);
            }
            else
            {
                v1 = 0;
                v2 = Convert.ToInt32(version2_parts[i]);
            }
            if (v1 > v2)
                return 1;
            return -1;

        }

        public static void AdjustAspectRatio(int width, int height, bool isThumbnail, out int adjustedWidth, out int adjustedHeight)
        {
            int maxHeight, maxWidth;
            if (isThumbnail)
            {
                maxHeight = HikeConstants.ATTACHMENT_THUMBNAIL_MAX_HEIGHT;
                maxWidth = HikeConstants.ATTACHMENT_THUMBNAIL_MAX_WIDTH;
            }
            else
            {
                maxHeight = HikeConstants.ATTACHMENT_MAX_HEIGHT;
                maxWidth = HikeConstants.ATTACHMENT_MAX_WIDTH;
            }

            if (height > width)
            {
                adjustedHeight = maxHeight;
                adjustedWidth = (width * adjustedHeight) / height;
            }
            else
            {
                adjustedWidth = maxWidth;
                adjustedHeight = (height * adjustedWidth) / width;
            }
        }

        public static string getAppVersion()
        {
            Uri manifest = new Uri("WMAppManifest.xml", UriKind.Relative);
            var si = Application.GetResourceStream(manifest);
            if (si != null)
            {
                using (StreamReader sr = new StreamReader(si.Stream))
                {
                    bool haveApp = false;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (!haveApp)
                        {
                            int i = line.IndexOf("AppPlatformVersion=\"", StringComparison.InvariantCulture);
                            if (i >= 0)
                            {
                                haveApp = true;
                                line = line.Substring(i + 20);
                                int z = line.IndexOf("\"");
                                if (z >= 0)
                                {
                                    // if you're interested in the app plat version at all                        
                                    // AppPlatformVersion = line.Substring(0, z);                      
                                }
                            }
                        }

                        int y = line.IndexOf("Version=\"", StringComparison.InvariantCulture);
                        if (y >= 0)
                        {
                            int z = line.IndexOf("\"", y + 9, StringComparison.InvariantCulture);
                            if (z >= 0)
                            {
                                // We have the version, no need to read on.                      
                                return line.Substring(y + 9, z - y - 9);
                            }
                        }
                    }
                }
            }

            return "Unknown";
        }

        public static string getOSVersion()
        {
            return System.Environment.OSVersion.Version.Major.ToString() + "." + System.Environment.OSVersion.Version.Minor.ToString()
                + "." + System.Environment.OSVersion.Version.Build.ToString();
        }

        //unique id for device. note:- it is not imei number
        public static string getHashedDeviceId()
        {
            object uniqueIdObj = null;
            byte[] uniqueId = null;
            if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out uniqueIdObj))
            {
                uniqueId = (byte[])uniqueIdObj;
            }
            string deviceId = uniqueId == null ? null : BitConverter.ToString(uniqueId);
            if (string.IsNullOrEmpty(deviceId))
                return null;
            deviceId = deviceId.Replace("-", "");
            return "wp:" + computeHash(deviceId);
        }

        private static string computeHash(string input)
        {
            string rethash = "";
            try
            {
                var sha = new SHA1Managed();
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] resultHash = sha.ComputeHash(bytes);
                rethash = Convert.ToBase64String(resultHash);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Utils ::  computeHash :  computeHash , Exception : " + ex.StackTrace);
            }
            return rethash;
        }

        //carrier DeviceNetworkInformation.CellularMobileOperator;

        public static string getDeviceModel()
        {
            string model = null;
            string manufacturer = null;

            object theModel = null;
            object manufacturerObj = null;

            if (Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceManufacturer", out manufacturerObj))
                manufacturer = manufacturerObj as string;
            if (Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceName", out theModel))
                model = theModel as string;

            return string.Format("{0} {1}", manufacturer ?? string.Empty, model ?? string.Empty);
        }

        public static JObject deviceInforForAnalytics()
        {
            JObject info = new JObject();
            info["_device"] = getDeviceModel();
            info["_app_version"] = getAppVersion();
            info[HikeConstants.TAG] = "cbs";
            info["_carrier"] = DeviceNetworkInformation.CellularMobileOperator;
            info["device_id"] = getHashedDeviceId();
            info[HikeConstants.OS_VERSION] = getOSVersion();
            info[HikeConstants.OS_NAME] = "win8";
            JObject infoPacket = new JObject();
            infoPacket[HikeConstants.DATA] = info;
            infoPacket[HikeConstants.TYPE] = HikeConstants.LOG_EVENT;
            return infoPacket;
        }

        public static bool IsIndianNumber(string msisdn)
        {
            if (msisdn == null)
                return false;
            if (msisdn.StartsWith(HikeConstants.INDIA_COUNTRY_CODE))
                return true;
            return false;
        }

        public static bool IsNumber(string charsEntered)
        {
            if (charsEntered.StartsWith("+")) // as in +91981 etc etc
            {
                charsEntered = charsEntered.Substring(1);
            }
            long i = 0;
            return long.TryParse(charsEntered, out i);
        }

        public static bool IsNumberValid(string charsEntered)
        {
            // TODO : Use regex if required
            // CASES 
            /*
             * 1. If number starts with '+'
             */

            if (charsEntered.StartsWith("+"))
            {
                if (charsEntered.Length < 2 || charsEntered.Length > 15)
                    return false;
            }
            else
            {
                if (charsEntered.Length < 1 || charsEntered.Length > 15)
                    return false;
            }
            return true;
        }

        public static string NormalizeNumber(string msisdn)
        {
            if (msisdn.StartsWith("+"))
            {
                return msisdn;
            }
            else if (msisdn.StartsWith("00"))
            {
                /*
                 * Doing for US numbers
                 */
                return "+" + msisdn.Substring(2);
            }
            else if (msisdn.StartsWith("0"))
            {
                string country_code = null;
                App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);
                return ((country_code == null ? HikeConstants.INDIA_COUNTRY_CODE : country_code) + msisdn.Substring(1));
            }
            else
            {
                string country_code2 = null;
                App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code2);
                return (country_code2 == null ? HikeConstants.INDIA_COUNTRY_CODE : country_code2) + msisdn;
            }
        }

        public static ConversationListObject GetConvlistObj(string msisdn)
        {
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                return App.ViewModel.ConvMap[msisdn];
            else
                return App.ViewModel.GetFav(msisdn);
        }

        public static bool IsHikeBotMsg(string msisdn)
        {
            return msisdn.Contains("hike");
        }

        public static string GetHikeBotName(string msisdn)
        {
            if (string.IsNullOrEmpty(msisdn))
                return string.Empty;
            switch (msisdn)
            {
                case HikeConstants.FTUE_HIKEBOT_MSISDN:
                    return "Emma from hike";
                case HikeConstants.FTUE_TEAMHIKE_MSISDN:
                    return "team hike";
                case HikeConstants.FTUE_GAMING_MSISDN:
                    return "Games on hike";
                case HikeConstants.FTUE_HIKE_DAILY_MSISDN:
                    return "hike daily";
                case HikeConstants.FTUE_HIKE_SUPPORT_MSISDN:
                    return "hike support";
                default:
                    return string.Empty;
            }
        }

        public static Uri LoadPageUri(App.PageState pageState)
        {
            Uri nUri = null;

            switch (pageState)
            {
                case App.PageState.WELCOME_SCREEN:
                    nUri = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
                    break;
                case App.PageState.PHONE_SCREEN:
                    App.createDatabaseAsync();
                    nUri = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
                    break;
                case App.PageState.PIN_SCREEN:
                    App.createDatabaseAsync();
                    nUri = new Uri("/View/EnterPin.xaml", UriKind.Relative);
                    break;
                case App.PageState.SETNAME_SCREEN:
                    App.createDatabaseAsync();
                    nUri = new Uri("/View/EnterName.xaml", UriKind.Relative);
                    break;
                case App.PageState.TUTORIAL_SCREEN_STATUS:
                case App.PageState.TUTORIAL_SCREEN_STICKERS:
                    nUri = new Uri("/View/TutorialScreen.xaml", UriKind.Relative);
                    break;
                case App.PageState.CONVLIST_SCREEN:
                    nUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                    break;
                case App.PageState.UPGRADE_SCREEN:
                    nUri = new Uri("/View/UpgradePage.xaml", UriKind.Relative);
                    break;
                default:
                    nUri = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
                    break;
            }
            return nUri;
        }

        public static string GetParamFromUri(string targetPage)
        {
            try
            {
                int idx = targetPage.IndexOf("msisdn");
                return targetPage.Substring(idx).Remove(0, 7);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("App :: GetParamFromUri : GetParamFromUri , Exception : " + ex.StackTrace);
                return "";
            }
        }

        public static bool IsUriStealth(string targetPage)
        {
            if (targetPage.Contains("sth"))
                return true;
            return false;
        }

        public static string GetFirstName(string completeName)
        {
            string firstName = string.Empty;
            if (!string.IsNullOrEmpty(completeName))
            {
                firstName = completeName.Split(' ')[0];
            }
            return firstName;
        }

        public enum Resolutions { Default, WVGA, WXGA, HD720p };

        private static Resolutions currentResolution = Resolutions.Default;
        private static Resolutions palleteResolution = Resolutions.Default;
        private static bool IsWvga
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 100;
            }

        }

        private static bool IsWxga
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 160;
            }
        }

        private static bool Is720p
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 150;
            }
        }

        public static Resolutions CurrentResolution
        {
            get
            {
                if (currentResolution == Resolutions.Default)
                {
                    if (IsWvga) currentResolution = Resolutions.WVGA;
                    else if (IsWxga) currentResolution = Resolutions.WXGA;
                    else if (Is720p) currentResolution = Resolutions.HD720p;
                    currentResolution = Resolutions.WVGA;
                }
                return currentResolution;
            }
        }

        public static Resolutions PalleteResolution
        {
            get
            {
                if (palleteResolution == Resolutions.Default)
                {
                    if (IsWvga) palleteResolution = Resolutions.WVGA;
                    else if (IsWxga) palleteResolution = Resolutions.WXGA;
                    else if (Is720p) palleteResolution = Resolutions.HD720p;
                    else
                        palleteResolution = Resolutions.WVGA;
                }
                return palleteResolution;
            }
        }
        public static void RequestServerEpochTime()
        {
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.REQUEST_SERVER_TIME;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        public static string ConvertToStorageSizeString(long sizeInBytes)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int i = 0;
            double dValue = (double)sizeInBytes;

            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0,2:n1} {1}", dValue, suffixes[i]);
        }

        public static void RequestHikeBot()
        {
            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.REQUEST_ACCOUNT_INFO);
            JObject data = new JObject();
            data.Add(HikeConstants.Extras.SEND_BOT, false);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        public static bool ShowNotificationAlert()
        {
            long lastNotificationTime = 0;
            appSettings.TryGetValue(HikeConstants.LAST_NOTIFICATION_TIME, out lastNotificationTime);

            return lastNotificationTime == 0 || ((DateTime.Now.Ticks - lastNotificationTime) / TimeSpan.TicksPerMillisecond > MIN_TIME_BETWEEN_NOTIFICATIONS);
        }

        public static string GetMessageStatus(ConvMessage.State state, JArray obj, int userCount, string id)
        {
            if (state == ConvMessage.State.SENT_DELIVERED_READ || state == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ)
                return GetReadBy(obj, userCount, id);
            else
                return String.Empty;
        }

        public static String GetReadBy(JArray obj, int userCount, string id)
        {
            if (obj == null)
                return AppResources.MessageStatus_ReadByEveryone;

            string readBy = "";

            var list = obj.ToObject<List<string>>();
            list = list.Distinct().ToList();

            if (list.Count == userCount)
                return AppResources.MessageStatus_ReadByEveryone;

            int count = 0;
            list.Reverse();

            if (list.Count > 3)
            {
                count = list.Count - 3;
                list.RemoveRange(3, count);
            }

            for (int i = 0; i < list.Count; i++)
            {
                GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, list[i], id);
                readBy += gp.FirstName;

                if (i == list.Count - 2)
                    readBy += " & ";
                else if (i < list.Count - 2)
                    readBy += ", ";
            }

            if (count == 0)
                readBy = string.Format(AppResources.MessageStatus_ReadByOneOrTwo, readBy);
            else if (count == 1)
                readBy = string.Format(AppResources.MessageStatus_ReadByThree, readBy);
            else
                readBy = string.Format(AppResources.MessageStatus_ReadByMoreThanThree, readBy, count);

            return readBy;
        }

        static public string GetFormattedTimeFromSeconds(long seconds)
        {
            long minute = seconds / 60;
            long secs = seconds % 60;
            return minute.ToString("00") + ":" + secs.ToString("00");
        }

        static public int GetMaxCharForBlock(string message, int maxLinesPerBlock = 35, int maxCharsPerLine = 30)
        {
            string trimmedMessage = message;
            int lineCount = 1;
            int charCount = 0;
            while (trimmedMessage.Length > 0)
            {
                char[] newLineChar = new char[] { '\r', '\n' };
                int index = trimmedMessage.IndexOfAny(newLineChar);
                if (index == -1)
                {
                    string currentString = trimmedMessage;
                    charCount += currentString.Length;
                    lineCount += Convert.ToInt32(Math.Floor(currentString.Length / (double)maxCharsPerLine));
                    if (lineCount > maxLinesPerBlock)
                    {
                        charCount -= maxCharsPerLine * (lineCount - maxLinesPerBlock - 1);
                        charCount -= currentString.Length % maxCharsPerLine;
                    }
                    break;
                }
                else if (index == 0)
                {
                    trimmedMessage = trimmedMessage.Substring(index + 1);
                    lineCount += 1;
                    charCount += 1;
                    if (lineCount > maxLinesPerBlock)
                        break;
                }
                else
                {
                    string currentString = trimmedMessage.Substring(0, index - 1);
                    charCount += currentString.Length + 2;
                    trimmedMessage = trimmedMessage.Substring(index + 1);
                    lineCount += 1 + Convert.ToInt32(Math.Floor(currentString.Length / (double)maxCharsPerLine));
                    if (lineCount > maxLinesPerBlock)
                    {
                        charCount -= (lineCount - maxLinesPerBlock - 2) > 0 ? maxCharsPerLine * (lineCount - maxLinesPerBlock - 2) : 0;
                        charCount -= currentString.Length % maxCharsPerLine;
                        break;
                    }
                }
            }
            return charCount;
        }
        public static void PlayFileInMediaPlayer(string fileLocation)
        {
            MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
            mediaPlayerLauncher.Media = new Uri(fileLocation, UriKind.Relative);
            mediaPlayerLauncher.Location = MediaLocationType.Data;
            mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
            mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
            try
            {
                mediaPlayerLauncher.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Utils.cs ::  PlayFileInMediaPlayer ,Audio video , Exception : " + ex.StackTrace);
            }
            return;
        }

        private static object _savePictureLock = new Object();
        public static bool SavePictureToLibrary(string newName, string isolatedStorageFilePath)
        {
            bool result;
            lock (_savePictureLock)
            {
                try
                {
                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        IsolatedStorageFileStream myFileStream = isoStore.OpenFile(isolatedStorageFilePath, FileMode.Open, FileAccess.Read);
                        MediaLibrary library = new MediaLibrary();
                        object temp = library.SavePicture(newName, myFileStream);
                        myFileStream.Close();
                        result = (temp != null) ? true : false;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Utils :: SavePictureToLibrary - Error on Saving file : " + isolatedStorageFilePath + ", Exception : " + e.StackTrace);
                    result = false;
                }
            }
            return result;
        }

        public static bool IsGZipHeader(byte[] arr)
        {
            return arr.Length >= 2 &&
                arr[0] == 31 &&
                arr[1] == 139;
        }


        /// <summary>
        /// It creates a file in Hike directory under Pictures folder.
        /// </summary>
        /// <param name="sourceFile">absolute path of file which we want to copy to Hike directory</param>
        /// <param name="targetFileName">name of the file in hike directory </param>
        public static async Task<bool> StoreFileInHikeDirectory(string sourceFile, string targetFileName)
        {
            bool result = true;

            if (!Directory.Exists(HikeConstants.HikeDirectoryPath))
                Directory.CreateDirectory(HikeConstants.HikeDirectoryPath);
            
            try
            {
                string targetFile = HikeConstants.HikeDirectoryName + "\\" + targetFileName;
                StorageFile source = await StorageFile.GetFileFromPathAsync(sourceFile);
                StorageFile target = await KnownFolders.PicturesLibrary.CreateFileAsync(targetFile, CreationCollisionOption.GenerateUniqueName);

                using (IRandomAccessStream inStream = await target.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (IRandomAccessStream outStream = await source.OpenAsync(FileAccessMode.Read))
                    {
                        Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer((uint)outStream.Size);
                        await outStream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                        await inStream.WriteAsync(buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception at Utils::SaveFileInHikeDirectory" + ex.StackTrace);
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Returns absolute path of a file in Isolated Storage
        /// </summary>
        /// <param name="filename">Path of the file in Isolated storage.</param>
        /// <returns></returns>
        public static string GetAbsolutePath(string filename)
        {
            string absoulutePath = null;

            try
            {
                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(filename))
                    {
                        using (IsolatedStorageFileStream output = new IsolatedStorageFileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, isoStore))
                        {
                            absoulutePath = output.Name;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Utils.cs ::  GetAbsolutePath , Exception : " + ex.StackTrace);
            }

            return absoulutePath;
        }

    }
}
