using CommonLibrary.Constants;
using CommonLibrary.DbUtils;
using CommonLibrary.Lib;
using CommonLibrary.Misc;
using CommonLibrary.Model;
using SharpCompress.Compressor;
using SharpCompress.Compressor.Deflate;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CommonLibrary.Utils
{
    public static class Utility
    {
        /// <summary>
        /// Read and calculate md5 by parts from file
        /// </summary>
        /// <param name="filePath">file path for file of which md5 needs to be calculated</param>
        /// <returns>md5 string</returns>
        public static string GetMD5Hash(string filePath)
        {
            byte[] buffer;
            byte[] oldBuffer;
            int bytesRead;
            int oldBytesRead;
            long size;
            long totalBytesRead = 0;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                    {
                        using (MD5 hashAlgorithm = MD5.Create())
                        {
                            size = fileStream.Length;
                            buffer = new byte[4096];
                            bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                            totalBytesRead += bytesRead;

                            do
                            {
                                oldBytesRead = bytesRead;
                                oldBuffer = buffer;

                                buffer = new byte[4096];
                                bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                                totalBytesRead += bytesRead;

                                if (bytesRead == 0)
                                    hashAlgorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                                else
                                    hashAlgorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);

                            } while (bytesRead != 0);

                            StringBuilder sb = new StringBuilder();

                            foreach (byte b in hashAlgorithm.Hash)
                                sb.Append(b.ToString("x2"));

                            return sb.ToString();
                        }
                    }
                }
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// -1 if v1 less v2
        /// 0 if v1 equal v2
        /// 1 is v1 greater v2
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static int CompareVersion(string version1, string version2)
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

        public static bool IsGroupConversation(string msisdn)
        {
            if (msisdn == HikeConstants.MY_PROFILE_PIC)
                return false;
            return !msisdn.StartsWith("+");
        }

        public static bool IsGZipHeader(byte[] arr)
        {
            return arr.Length >= 2 &&
                arr[0] == 31 &&
                arr[1] == 139;
        }

        public static string GZipDecompress(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
                return string.Empty;

            //Prepare for decompress
            MemoryStream ms = new MemoryStream(byteArray);
            GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress);

            //Decompress
            byte[] buffer = StreamToByteArray(gzip);

            //Transform byte[] unzip data to string
            StringBuilder sb = new StringBuilder();

            //Read the number of bytes GZipStream red and do not a for each bytes in resultByteArray;
            for (int i = 0; i < buffer.Length; i++)
                sb.Append((char)buffer[i]);

            gzip.Close();
            ms.Close();

            gzip.Dispose();
            ms.Dispose();

            return sb.ToString();
        }

        public static byte[] StreamToByteArray(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            MemoryStream ms = new MemoryStream();

            int read;

            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            return ms.ToArray();
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

        public static string GetAppVersion()
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

        public static int GetRecieverMoodId(int currentMoodId)
        {
            if (currentMoodId > 33)
                currentMoodId -= 9;

            return currentMoodId;
        }

        public static ContactInfo GetContactInfo(string msisdn)
        {
            ContactInfo contactInfo = null;

            if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(msisdn))
                contactInfo = HikeInstantiation.ViewModel.ContactsCache[msisdn];
            else
            {
                contactInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);

                if (contactInfo != null)
                    HikeInstantiation.ViewModel.ContactsCache[msisdn] = contactInfo;
            }

            return contactInfo;
        }

        public static bool CheckUserInAddressBook(string msisdn)
        {
            bool inAddressBook = false;
            ConversationListObject convObj;
            ContactInfo cinfo;

            if (HikeInstantiation.ViewModel.ConvMap.TryGetValue(msisdn, out convObj) && (convObj.ContactName != null))
                inAddressBook = true;
            else if (HikeInstantiation.ViewModel.ContactsCache.TryGetValue(msisdn, out cinfo) && cinfo.Name != null)
                inAddressBook = true;
            else if (UsersTableUtils.getContactInfoFromMSISDN(msisdn) != null)
                inAddressBook = true;

            return inAddressBook;

        }

        public static string ConvertUrlToFileName(string url)
        {
            var restrictedCharaters = new[] { '/', '\\', '*', '"', '|', '<', '>', ':', '?', '.' };
            url = restrictedCharaters.Aggregate(url, (current, restrictedCharater) => current.Replace(restrictedCharater, '_'));

            return url;
        }

        public static string[] SplitUserJoinedMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return null;
            char[] delimiters = new char[] { ',' };
            return msg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
