using System;
using System.Net;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SharpCompress.Compressor.Deflate;
using SharpCompress.Compressor;
using System.Text;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using windows_client.Controls;
using System.Threading;
using windows_client.DbUtils;
using windows_client.Misc;

namespace windows_client.utils
{
    public class AccountUtils
    {
        private static readonly bool IS_PRODUCTION = false;

        private static readonly string PRODUCTION_HOST = "api.im.hike.in";

        //private static readonly string STAGING_HOST = "migration.im.hike.in";
        private static readonly string STAGING_HOST = "staging.im.hike.in";

        private static readonly string MQTT_HOST_SERVER = "mqtt.im.hike.in";

        private static readonly string FILE_TRANSFER_HOST = "ft.im.hike.in";

        private static readonly int PRODUCTION_PORT = 80;

        private static readonly int STAGING_PORT = 8080;

        public static bool IsProd
        {
            get
            {
                //bool isStaging;
                //if (!App.appSettings.TryGetValue<bool>(HikeConstants.STAGING_SERVER, out isStaging))
                //    isStaging = !IS_PRODUCTION;
                return App.IS_MARKETPLACE ? true : IS_PRODUCTION;
            }
            //set
            //{
            //    bool isStaging;
            //    App.appSettings.TryGetValue(HikeConstants.STAGING_SERVER, out isStaging);
            //    if (value != isStaging)
            //        App.WriteToIsoStorageSettings(HikeConstants.STAGING_SERVER,value);
            //}
        }

        #region MQTT RELATED

        public static string MQTT_HOST
        {
            get
            {
                if (IsProd)
                    return MQTT_HOST_SERVER;
                return STAGING_HOST;
            }
        }

        public static int MQTT_PORT
        {
            get
            {
                if (IsProd)
                    return STAGING_PORT;
                return 1883;
            }
        }

        public static string FILE_TRANSFER_BASE
        {
            get
            {
                if (IsProd)
                    return "http://" + FILE_TRANSFER_HOST + ":" + Convert.ToString(PORT) + "/v1";
                return "http://" + STAGING_HOST + ":" + Convert.ToString(STAGING_PORT) + "/v1";
            }
        }

        #endregion

        public static string HOST = IsProd ? PRODUCTION_HOST : STAGING_HOST;

        public static int PORT = IsProd ? PRODUCTION_PORT : STAGING_PORT;

        public static readonly string BASE = "http://" + HOST + ":" + Convert.ToString(PORT) + "/v1";
        public static readonly string AVATAR_BASE = "http://" + HOST + ":" + Convert.ToString(PORT);

        public static readonly string NETWORK_PREFS_NAME = "NetworkPrefs";

        public static string mToken = null;
        private static string uid = null;

        public class AccountInfo
        {
            public string token;

            public string msisdn;

            public string uid;

            public int smsCredits;

            public AccountInfo(string token, string msisdn, string uid, int smsCredits)
            {
                this.token = token;
                this.msisdn = msisdn;
                this.uid = uid;
                this.smsCredits = smsCredits;
            }
        }

        public static string UID
        {
            get { return uid; }
            set
            {
                if (value != mToken)
                {
                    uid = value;
                }
            }
        }
        public static string Token
        {
            get { return mToken; }
            set
            {
                if (value != mToken)
                {
                    mToken = value;
                }
            }
        }



        public delegate void postResponseFunction(JObject obj);
        public delegate void downloadFile(byte[] downloadedData, object metadata);
        public delegate void postUploadPhotoFunction(JObject obj, ConvMessage convMessage, SentChatBubble chatBubble);


        private enum RequestType
        {
            REGISTER_ACCOUNT, INVITE, VALIDATE_NUMBER, CALL_ME, SET_NAME, DELETE_ACCOUNT, POST_ADDRESSBOOK, UPDATE_ADDRESSBOOK, POST_PROFILE_ICON,
            POST_PUSHNOTIFICATION_DATA, UPLOAD_FILE, SET_PROFILE, SOCIAL_POST, SOCIAL_DELETE, POST_STATUS
        }
        private static void addToken(HttpWebRequest req)
        {
            req.Headers["Cookie"] = "user=" + mToken + ";uid=" + (string)App.appSettings[App.UID_SETTING];
        }

        public static void registerAccount(string pin, string unAuthMSISDN, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.Headers[HttpRequestHeader.ContentEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.REGISTER_ACCOUNT, pin, unAuthMSISDN, finalCallbackFunction });
        }

        public static void postAddressBook(Dictionary<string, List<ContactInfo>> contactListMap, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/addressbook")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers["Accept-Encoding"] = "gzip";
            req.Headers["Content-Encoding"] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.POST_ADDRESSBOOK, contactListMap, finalCallbackFunction });
        }

        public static void updateAddressBook(Dictionary<string, List<ContactInfo>> contacts_to_update_or_add, JArray ids_to_delete, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/addressbook")) as HttpWebRequest;
            addToken(req);
            req.Method = "PATCH";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.UPDATE_ADDRESSBOOK, contacts_to_update_or_add, ids_to_delete, finalCallbackFunction });
        }

        public static void invite(string phone_no, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/user/invite")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.INVITE, phone_no, finalCallbackFunction });
        }

        public static void validateNumber(string phoneNo, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/validate?digits=4")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.VALIDATE_NUMBER, phoneNo, finalCallbackFunction });
        }

        public static void postForCallMe(string msisdn, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/pin-call")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.CALL_ME, msisdn, finalCallbackFunction });
        }

        public static void setName(string name, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/name")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SET_NAME, name, finalCallbackFunction });
        }

        public static void setGroupName(string name, string grpId, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + string.Format("/group/{0}/name", grpId))) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SET_NAME, name, finalCallbackFunction });
        }

        public static void setProfile(JObject obj, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/profile")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SET_PROFILE, obj, finalCallbackFunction });
        }

        public static void deleteRequest(postResponseFunction finalCallbackFunction, string requestUrl)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            addToken(req);
            req.Method = "DELETE";
            addToken(req);
            req.BeginGetResponse(json_Callback, new object[] { req, RequestType.DELETE_ACCOUNT, finalCallbackFunction });
        }
        public static void unlinkAccount(postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/unlink")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            addToken(req);
            req.BeginGetResponse(json_Callback, new object[] { req, RequestType.DELETE_ACCOUNT, finalCallbackFunction });
        }
        public static void updateProfileIcon(byte[] buffer, postResponseFunction finalCallbackFunction, string groudId)
        {
            Uri requestUri;
            if (String.IsNullOrEmpty(groudId))
                requestUri = new Uri(BASE + "/account/avatar");
            else
                requestUri = new Uri(BASE + "/group/" + groudId + "/avatar");

            HttpWebRequest req = HttpWebRequest.Create(requestUri) as HttpWebRequest;
            addToken(req);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.POST_PROFILE_ICON, buffer, finalCallbackFunction });
        }

        public static void postPushNotification(string uri, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/device")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.POST_PUSHNOTIFICATION_DATA, uri, finalCallbackFunction });
        }

        public static void uploadFile(byte[] dataBytes, postUploadPhotoFunction finalCallbackFunction, ConvMessage convMessage,
            SentChatBubble chatbubble)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(HikeConstants.FILE_TRANSFER_BASE_URL)) as HttpWebRequest;
            addToken(req);
            req.Method = "PUT";
            req.ContentType = convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE) ||
                convMessage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO) ? "" : convMessage.FileAttachment.ContentType;
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = convMessage.FileAttachment.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";

            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.UPLOAD_FILE, dataBytes, finalCallbackFunction, convMessage, 
                chatbubble });
        }

        public static void postStatus(string statusText, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/user/status")) as HttpWebRequest;
            addToken(req);
            req.Method = "PUT";
            req.ContentType = "";
            req.Headers["hike-status-message"] = statusText;
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.POST_STATUS, finalCallbackFunction });
        }


        public static void SocialPost(JObject obj, postResponseFunction finalCallbackFunction, string socialNetowrk, bool isPost)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/connect/" + socialNetowrk)) as HttpWebRequest;
            addToken(req);
            if (isPost)
            {
                req.Method = "POST";
                req.ContentType = "application/json";
                req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SOCIAL_POST, obj, finalCallbackFunction });
            }
            else
            {
                req.Method = "DELETE";
                req.BeginGetResponse(json_Callback, new object[] { req, RequestType.SOCIAL_DELETE, finalCallbackFunction });
            }
        }

        private static void setParams_Callback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            JObject data = new JObject();
            HttpWebRequest req = vars[0] as HttpWebRequest;
            Stream postStream = req.EndGetRequestStream(result);
            postResponseFunction finalCallbackFunction = null;
            RequestType type = (RequestType)vars[1];

            switch (type)
            {
                #region REGISTER ACCOUNT
                case RequestType.REGISTER_ACCOUNT:
                    string pin = vars[2] as string;
                    string unAuthMSISDN = vars[3] as string;
                    finalCallbackFunction = vars[4] as postResponseFunction;
                    data.Add("set_cookie", "0");
                    data.Add("devicetype", "windows");
                    data[HikeConstants.DEVICE_ID] = Utils.getHashedDeviceId();
                    //data[HikeConstants.DEVICE_TOKEN] = Utils.getDeviceId();//for push notifications
                    data[HikeConstants.DEVICE_VERSION] = Utils.getDeviceModel();
                    data[HikeConstants.APP_VERSION] = Utils.getAppVersion();
                    string inviteToken = "";
                    if (!string.IsNullOrEmpty(inviteToken))
                        data[HikeConstants.INVITE_TOKEN_KEY] = inviteToken;
                    if (pin != null)
                    {
                        data.Add("msisdn", unAuthMSISDN);
                        data.Add("pin", pin);
                    }
                    Compress4(data.ToString(Formatting.None), postStream);
                    postStream.Close();
                    req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallbackFunction });
                    return;
                #endregion
                #region INVITE
                case RequestType.INVITE:
                    string phoneNo = vars[2] as string;
                    data.Add("to", phoneNo);
                    break;
                #endregion
                #region VALIDATE NUMBER
                case RequestType.VALIDATE_NUMBER:
                    string numberToValidate = vars[2] as string;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data.Add("phone_no", numberToValidate);
                    break;
                #endregion
                #region CALL ME
                case RequestType.CALL_ME:
                    string msisdn = vars[2] as string;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data.Add("msisdn", msisdn);
                    break;
                #endregion
                #region SET NAME
                case RequestType.SET_NAME:
                    string nameToSet = vars[2] as string;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data.Add("name", nameToSet);
                    break;
                #endregion
                #region SET PROFILE
                case RequestType.SET_PROFILE:
                    JObject jo = vars[2] as JObject;
                    data = jo;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    break;
                #endregion
                #region POST ADDRESSBOOK
                case RequestType.POST_ADDRESSBOOK:
                    Dictionary<string, List<ContactInfo>> contactListMap = vars[2] as Dictionary<string, List<ContactInfo>>;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data = getJsonContactList(contactListMap);
                    string x = data.ToString(Newtonsoft.Json.Formatting.None);
                    Compress4(x,postStream);
                    //Debug.WriteLine("Request gets compressed from {0} to {1} Length", x.Length, d.Length);
                    //using (StreamWriter sw = new StreamWriter(postStream))
                    //{
                    //    sw.Write(d);
                    //    sw.Flush();
                    //    //postStream.Flush();
                    //}
                    postStream.Close();
                    req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallbackFunction });
                    return;
                    break;
                #endregion
                #region SOCIAL POST
                case RequestType.SOCIAL_POST:
                    data = vars[2] as JObject;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    break;
                #endregion
                #region SOCIAL DELETE
                case RequestType.SOCIAL_DELETE:
                    finalCallbackFunction = vars[2] as postResponseFunction;
                    break;
                #endregion
                #region UPDATE ADDRESSBOOK
                case RequestType.UPDATE_ADDRESSBOOK:
                    Dictionary<string, List<ContactInfo>> contacts_to_update = vars[2] as Dictionary<string, List<ContactInfo>>;
                    JArray ids_json = vars[3] as JArray;
                    finalCallbackFunction = vars[4] as postResponseFunction;
                    if (ids_json != null)
                        data.Add("remove", ids_json);
                    JObject ids_to_update = getJsonContactList(contacts_to_update);
                    if (ids_to_update != null)
                        data.Add("update", ids_to_update);
                    break;
                #endregion
                #region DELETE ACCOUNT
                case RequestType.DELETE_ACCOUNT:
                    finalCallbackFunction = vars[2] as postResponseFunction;
                    break;
                #endregion
                #region POST PROFILE ICON
                case RequestType.POST_PROFILE_ICON:
                    byte[] imageBytes = (byte[])vars[2];
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    postStream.Write(imageBytes, 0, imageBytes.Length);
                    postStream.Close();
                    req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallbackFunction });
                    return;
                #endregion
                #region POST PUSH NOTIFICATION DATA
                case RequestType.POST_PUSHNOTIFICATION_DATA:
                    string uri = (string)vars[2];
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data.Add("dev_token", uri);
                    data.Add("dev_type", "windows");
                    break;
                #endregion
                #region UPLOAD FILE
                case RequestType.UPLOAD_FILE:
                    byte[] dataBytes = (byte[])vars[2];
                    postUploadPhotoFunction finalCallbackForUploadFile = vars[3] as postUploadPhotoFunction;
                    ConvMessage convMessage = vars[4] as ConvMessage;
                    SentChatBubble chatBubble = vars[5] as SentChatBubble;
                    int bufferSize = 2048;
                    int startIndex = 0;
                    int noOfBytesToWrite = 0;
                    double progressValue = 0;
                    while (startIndex < dataBytes.Length)
                    {
                        Thread.Sleep(5);
                        noOfBytesToWrite = dataBytes.Length - startIndex;
                        noOfBytesToWrite = noOfBytesToWrite < bufferSize ? noOfBytesToWrite : bufferSize;
                        postStream.Write(dataBytes, startIndex, noOfBytesToWrite);
                        progressValue = ((double)(startIndex + noOfBytesToWrite) / dataBytes.Length) * 100;
                        bool updated = chatBubble.updateProgress(progressValue);
                        if (!updated)
                        {
                            chatBubble.setAttachmentState(Attachment.AttachmentState.CANCELED);
                            break;
                        }
                        startIndex += noOfBytesToWrite;
                    }

                    postStream.Close();
                    req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallbackForUploadFile, convMessage, chatBubble });
                    return;
                #endregion
                #region POST STATUS
                case RequestType.POST_STATUS:
                    finalCallbackFunction = vars[2] as postResponseFunction;
                    break;
                #endregion
                #region DEFAULT
                default:
                    break;
                #endregion
            }

            using (StreamWriter sw = new StreamWriter(postStream))
            {
                string json = data.ToString(Newtonsoft.Json.Formatting.None);
                sw.Write(json);
            }
            postStream.Close();
            req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallbackFunction });
        }

        //GET request
        public static void createGetRequest(string requestUrl, postResponseFunction callback, bool isRelativeUrl)
        {
            HttpWebRequest request = null;
            if (isRelativeUrl)
            {
                request = (HttpWebRequest)HttpWebRequest.Create(BASE + requestUrl);
            }
            else
            {
                request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            }
            request.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();//to disaable caching if GET result
            request.BeginGetResponse(GetRequestCallback, new object[] { request, callback });
        }

        public static void createGetRequest(string requestUrl, downloadFile callback, bool setCookie, object metadata)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            if (setCookie)
                addToken(request);
            request.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
            request.BeginGetResponse(GetRequestCallback, new object[] { request, callback, metadata });
        }

        static void GetRequestCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            HttpWebRequest request = vars[0] as HttpWebRequest;
            JObject jObject = null;
            string data = "";
            byte[] fileBytes = null;
            if (request != null)
            {
                try
                {
                    WebResponse response = request.EndGetResponse(result);
                    Stream responseStream = response.GetResponseStream();
                    if (string.Equals(response.Headers[HttpRequestHeader.ContentEncoding], "gzip", StringComparison.OrdinalIgnoreCase))
                    {
                        data = decompressResponse(responseStream);
                    }
                    else
                    {
                        if (vars[1] is postResponseFunction)
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                data = reader.ReadToEnd();
                            }
                            jObject = JObject.Parse(data);
                        }
                        else if (vars[1] is downloadFile)
                        {
                            using (BinaryReader br = new BinaryReader(responseStream))
                            {
                                fileBytes = br.ReadBytes((int)responseStream.Length);
                            }
                        }
                    }
                }
                catch (IOException ioe)
                {
                }
                catch (WebException we)
                {
                }
                catch (JsonException je)
                {
                }
                catch (Exception e)
                {
                }
                finally
                {
                    if (vars[1] is postResponseFunction)
                    {
                        postResponseFunction finalCallbackFunction = vars[1] as postResponseFunction;
                        finalCallbackFunction(jObject);
                    }
                    else if (vars[1] is downloadFile)
                    {
                        downloadFile downloadFileCallback = vars[1] as downloadFile;
                        downloadFileCallback(fileBytes, vars[2] as object);
                    }
                }
            }
        }

        public static string Decompress(string compressedText)
        {
            byte[] byteArray;

            //Transform string into byte[]
            try
            {
                byteArray = Convert.FromBase64String(compressedText);
            }
            catch
            {
                return compressedText;
            }

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

        public static byte[] Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
                gZipStream.Flush();
            }
            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            byte[] gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return gZipBuffer;
        }

        public static byte[] Compress3(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            using (var memoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(buffer, 0, buffer.Length);
                    gZipStream.Flush();
                }
                // memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Flush();
                return memoryStream.ToArray();
            }
        }

        public static void Compress4(string text, Stream postStream)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            using (var gZipStream = new GZipStream(postStream, CompressionMode.Compress))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
                gZipStream.Flush();
            }
            // memoryStream.Seek(0, SeekOrigin.Begin);
            //postStream.Flush();
            return;
        }

        public static byte[] Compress2(string text)
        {
            // if (text.Length < 300)
            // return text;

            //Transform string into byte[]  
            byte[] byteArray = new byte[text.Length];

            int index = 0;

            //Redo this w/o ToCharArray conversion
            foreach (char item in text.ToCharArray())
                byteArray[index++] = (byte)item;

            //Prepare for compress
            MemoryStream ms = new MemoryStream();
            GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true);

            //Compress
            gzip.Write(byteArray, 0, byteArray.Length);


            //Transform byte[] zip data to string
            byteArray = ms.ToArray();
            gzip.Flush();
            ms.Flush();
            gzip.Close();
            ms.Close();
            gzip.Dispose();
            ms.Dispose();
            return byteArray;
            //return Convert.ToBase64String(byteArray);
        }

        private static string decompressResponse(Stream responseStream)
        {
            string data;
            using (var outStream = new MemoryStream())
            using (var zipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            {
                zipStream.CopyTo(outStream);
                outStream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(outStream, Encoding.UTF8))
                {
                    data = reader.ReadToEnd();
                }
            }
            return data;
        }

        private static void json_Callback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            RequestType type = (RequestType)vars[1];

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            string data;
            JObject obj = null;
            ConvMessage convMessage;
            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                Stream responseStream = response.GetResponseStream();
                if (string.Equals(response.Headers[HttpRequestHeader.ContentEncoding], "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    data = decompressResponse(responseStream);
                }
                else
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        data = reader.ReadToEnd();
                    }
                }
                obj = JObject.Parse(data);
            }
            catch (IOException ioe)
            {
                obj = null;
            }
            catch (WebException we)
            {
                obj = null;
            }
            catch (JsonException je)
            {
                obj = null;
            }
            catch (Exception e)
            {
                obj = null;
            }
            finally
            {
                if (vars[2] is postResponseFunction)
                {
                    postResponseFunction finalCallbackFunction = vars[2] as postResponseFunction;
                    finalCallbackFunction(obj);
                }
                else if (vars[2] is postUploadPhotoFunction)
                {
                    postUploadPhotoFunction finalCallbackFunctionForUpload = vars[2] as postUploadPhotoFunction;
                    convMessage = vars[3] as ConvMessage;
                    SentChatBubble chatBubble = vars[4] as SentChatBubble;
                    finalCallbackFunctionForUpload(obj, convMessage, chatBubble);
                }
            }
        }

        public static List<string> getBlockList(JObject obj)
        {
            try
            {
                if ((obj == null) || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
                {
                    return null;
                }

                JArray blocklist = (JArray)obj["blocklist"];

                if (blocklist == null)
                {
                    return null;
                }
                List<string> blockListMsisdns = new List<string>();
                foreach (string num in blocklist)
                {
                    blockListMsisdns.Add(num);
                }
                return blockListMsisdns;
            }
            catch (ArgumentException e)
            {
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static JObject getJsonContactList(Dictionary<string, List<ContactInfo>> contactsMap)
        {
            JObject updateContacts = new JObject();
            foreach (string id in contactsMap.Keys)
            {
                List<ContactInfo> list = contactsMap[id];
                JArray contactInfoList = new JArray();
                foreach (ContactInfo cInfo in list)
                {
                    JObject contactInfo = new JObject();
                    contactInfo.Add("name", cInfo.Name);
                    contactInfo.Add("phone_no", cInfo.PhoneNo);
                    contactInfoList.Add(contactInfo);
                }
                updateContacts.Add(id, contactInfoList);
            }
            return updateContacts;
        }

        public static List<ContactInfo> getContactList(JObject obj, Dictionary<string, List<ContactInfo>> new_contacts_by_id, bool isRefresh)
        {
            try
            {
                if ((obj == null) || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
                {
                    return null;
                }
                JObject addressbook = (JObject)obj["addressbook"];
                if (addressbook == null)
                {
                    return null;
                }
                bool isFavSaved = false;
                bool isPendingSaved = false;
                int hikeCount = 1, smsCount = 1;
                List<ContactInfo> msgToShow = null;
                List<string> msisdns = null;
                if (!isRefresh)
                {
                    msgToShow = new List<ContactInfo>(5);
                    msisdns = new List<string>();
                }
                else // if refresh case load groups in cache
                {
                    GroupManager.Instance.LoadGroupCache();
                }

                List<ContactInfo> server_contacts = new List<ContactInfo>();
                IEnumerator<KeyValuePair<string, JToken>> keyVals = addressbook.GetEnumerator();
                KeyValuePair<string, JToken> kv;
                int count = 0;
                int totalContacts = 0;

                while (keyVals.MoveNext())
                {
                    kv = keyVals.Current;
                    JArray entries = (JArray)addressbook[kv.Key];
                    List<ContactInfo> cList = new_contacts_by_id[kv.Key];
                    for (int i = 0; i < entries.Count; ++i)
                    {
                        JObject entry = (JObject)entries[i];
                        string msisdn = (string)entry["msisdn"];
                        if (string.IsNullOrWhiteSpace(msisdn))
                        {
                            count++;
                            continue;
                        }
                        bool onhike = (bool)entry["onhike"];
                        ContactInfo cinfo = cList[i];
                        ContactInfo cn = new ContactInfo(kv.Key, msisdn, cinfo.Name, onhike, cinfo.PhoneNo);

                        if (!isRefresh) // this is case for new installation
                        {
                            if (cn.Msisdn != (string)App.appSettings[App.MSISDN_SETTING]) // do not add own number
                            {
                                if (onhike && hikeCount <= 3 && !msisdns.Contains(cn.Msisdn))
                                {
                                    msisdns.Add(cn.Msisdn);
                                    msgToShow.Add(cn);
                                    hikeCount++;
                                }
                                if (!onhike && smsCount <= 2 && cn.Msisdn.StartsWith("+91") && !msisdns.Contains(cn.Msisdn)) // allow only indian numbers for sms
                                {
                                    msisdns.Add(cn.Msisdn);
                                    msgToShow.Add(cn);
                                    smsCount++;
                                }
                            }
                        }
                        else // this is refresh contacts case
                        {
                            if (App.ViewModel.ConvMap.ContainsKey(cn.Msisdn)) // update convlist
                            {
                                try
                                {
                                    App.ViewModel.ConvMap[cn.Msisdn].ContactName = cn.Name;
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine("REFRESH CONTACTS :: Update contact exception " + e.StackTrace);
                                }
                            }
                            else // fav and pending case
                            {
                                ConversationListObject c = App.ViewModel.GetFav(cn.Msisdn);
                                if (c != null) // this user is in favs
                                {
                                    c.ContactName = cn.Name;
                                    MiscDBUtil.SaveFavourites(c);
                                    isFavSaved = true;
                                }
                                else
                                {
                                    c = App.ViewModel.GetPending(cn.Msisdn);
                                    if (c != null)
                                    {
                                        c.ContactName = cn.Name;
                                        isPendingSaved = true;
                                    }
                                }
                            }
                            GroupManager.Instance.RefreshGroupCache(cn);
                        }
                        server_contacts.Add(cn);
                        totalContacts++;
                    }
                }
                if (isFavSaved)
                    MiscDBUtil.SaveFavourites();
                if (isPendingSaved)
                    MiscDBUtil.SavePendingRequests();
                msisdns = null;
                Debug.WriteLine("Total contacts with no msisdn : {0}", count);
                Debug.WriteLine("Total contacts inserted : {0}", totalContacts);
                if (!isRefresh)
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.CONTACTS_TO_SHOW, msgToShow);
                return server_contacts;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }



    }
}
