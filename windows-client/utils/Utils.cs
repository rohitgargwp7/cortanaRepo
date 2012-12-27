using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Collections.Generic;
using windows_client.DbUtils;
using System;
using System.Diagnostics;
using System.Windows;
using System.IO;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using System.Security.Cryptography;

namespace windows_client.utils
{
    public class Utils
    {


        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public static void savedAccountCredentials(JObject obj)
        {
            JToken secure_push = null;
            if (obj.TryGetValue(HikeConstants.SECURE_PUSH,out secure_push))
            {
                appSettings[HikeConstants.SECURE_PUSH] = secure_push.ToObject<bool>();
            }
            App.MSISDN = (string)obj["msisdn"];
            AccountUtils.Token = (string)obj["token"];
            appSettings[App.MSISDN_SETTING] = App.MSISDN;
            appSettings[App.UID_SETTING] = (string)obj["uid"];
            appSettings[App.TOKEN_SETTING] = (string)obj["token"];
            appSettings[App.SMS_SETTING] = (int)obj[NetworkManager.SMS_CREDITS];
            appSettings[App.IS_PUSH_ENABLED] = (bool)true;
            appSettings[App.VIBRATE_PREF] = (bool)true;
            appSettings[App.LAST_UPDATE_CHECK_TIME] = (long)-1;
            appSettings[App.LAST_ANALYTICS_POST_TIME] = (long)TimeUtils.getCurrentTimeStamp();
            appSettings.Save();
        }

        public static bool isGroupConversation(string msisdn)
        {
            return !msisdn.StartsWith("+");
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
                requestAccountInfo.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.REQUEST_ACCOUNT_INFO);
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, requestAccountInfo);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Utils", "Invalid JSON", e);
            }
        }

        public static bool isDarkTheme()
        {
            return ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == Visibility.Visible);
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
        /// -1 if v1 < v2
        /// 0 if v1=v2
        /// 1 is v1>v2
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static int compareVersion(string version1, string version2)
        {
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

        public static bool isCriticalUpdatePending()
        {
            try
            {
                string lastCriticalVersion = "";
                App.appSettings.TryGetValue<string>(App.LAST_CRITICAL_VERSION, out lastCriticalVersion);
                if (String.IsNullOrEmpty(lastCriticalVersion))
                    return false;
                string currentVersion = Utils.getAppVersion();
                return compareVersion(lastCriticalVersion, currentVersion) == 1;
            }
            catch (Exception)
            {
                return false;
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
                uniqueId = (byte [])uniqueIdObj;
            }
            string deviceId = uniqueId==null?null:BitConverter.ToString(uniqueId);
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
                var sha = new HMACSHA1();
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] resultHash = sha.ComputeHash(bytes);
                rethash = Convert.ToBase64String(resultHash);
            }
            catch (Exception ex)
            {
            }
            return rethash; 
        }

        //carrier DeviceNetworkInformation.CellularMobileOperator;

        public static string getDeviceModel()
        {
            string model = null;
            object theModel = null;

            if (Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceName", out theModel))
                model = theModel as string;

            return model;
        }

        public static JObject deviceInforForAnalytics()
        {
            JObject info = new JObject();
            info["_device"] = getDeviceModel();
            info["_app_version"] = getAppVersion();
            info["tag"] = "cbs";
            info["_carrier"] = DeviceNetworkInformation.CellularMobileOperator;
            info["device_id"] = getHashedDeviceId();
            info["_os_version"] = getOSVersion();
            info["_os"] = "windows";
            JObject infoPacket = new JObject();
            infoPacket[HikeConstants.DATA] = info;
            infoPacket[HikeConstants.TYPE] = HikeConstants.LOG_EVENT;
            return infoPacket;
        }

        public static bool IsIndianNumber(string msisdn)
        {
            if (msisdn == null)
                return false;
            if (msisdn.StartsWith("+91"))
                return true;
            return false;
        }
    }
}
