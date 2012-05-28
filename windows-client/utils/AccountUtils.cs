using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using SharpCompress.Archive;
using SharpCompress.Common;
using SharpCompress.Reader;
using SharpCompress.Writer;
using SharpCompress.Writer.GZip;

namespace windows_client.utils
{
    public class AccountUtils
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly String HOST = "ec2-175-41-153-127.ap-southeast-1.compute.amazonaws.com";

        private static readonly int PORT = 3001;

        private static readonly String BASE = "http://" + HOST + ":" + Convert.ToString(PORT) + "/v1";

        public static readonly String NETWORK_PREFS_NAME = "NetworkPrefs";

        private static String mToken = null;

        public class AccountInfo
        {
            public String token;

            public String msisdn;

            public String uid;

            public int smsCredits;

            public AccountInfo(String token, String msisdn, String uid, int smsCredits)
            {
                this.token = token;
                this.msisdn = msisdn;
                this.uid = uid;
                this.smsCredits = smsCredits;
            }
        }

        public delegate void postResponseFunction(JObject obj);

        private enum RequestType
        {
            REGISTER_ACCOUNT, INVITE, VALIDATE_NUMBER, SET_NAME, DELETE
        }
        private static void addToken(HttpWebRequest req)
        {
            req.Headers["Cookie"] = "user=" + mToken;
        }

        public static void registerAccount(String pin, String unAuthMSISDN, postResponseFunction finalCallback)
        {

            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.REGISTER_ACCOUNT, pin, unAuthMSISDN, finalCallback });            
        }

        public static void invite(string phone_no)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/user/invite")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.INVITE, phone_no });
        }

        public static void validateNumber(string phoneNo, postResponseFunction finalCallback)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/validate")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.VALIDATE_NUMBER, phoneNo, finalCallback});
        }

        public static void setName(String name)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/name")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SET_NAME, name });
        }

        public static void deleteAccount()
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            req.Method = "DELETE";
            addToken(req);
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.DELETE });
        }
        private static void setParams_Callback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            JObject data = new JObject();
            HttpWebRequest req = vars[0] as HttpWebRequest;
            Stream postStream = req.EndGetRequestStream(result);
            postResponseFunction finalCallback = null;
            RequestType type = (RequestType)vars[1];
            switch (type)
            {
                case RequestType.REGISTER_ACCOUNT:
                    string pin = vars[2] as string;
                    string unAuthMSISDN = vars[3] as string;
                    finalCallback = vars[4] as postResponseFunction;
                    data.Add("set_cookie","0");
                    if (pin != null)
                    {
                        data.Add("msisdn", unAuthMSISDN);
                        data.Add("pin", pin);
                    }
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        //byte [] requestString= Encoding.UTF8.GetBytes(json);
                        
                        using (var zipW = WriterFactory.Open(sw.BaseStream, ArchiveType.GZip, CompressionType.GZip))
                        {
                        }
                        sw.Write(json);
                    }
                    break;

                case RequestType.INVITE:
                    string phoneNo = vars[2] as string;
                    
                    data.Add("to",phoneNo);
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        sw.Write(json);
                    }
                    break;

                case RequestType.VALIDATE_NUMBER:
                    string numberToValidate = vars[2] as string;
                    finalCallback = vars[3] as postResponseFunction;
                    data.Add("phone_no", numberToValidate);
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        sw.Write(json);
                    }
                    break;

                case RequestType.SET_NAME:
                    string nameToSet = vars[2] as string;
                    data.Add("name", nameToSet);
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        sw.Write(json);
                    }
                    break;
                default:
                    break;
            }
            postStream.Close();
            req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallback });
        }
        private static void json_Callback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            postResponseFunction finalCallback = vars[2] as postResponseFunction;
            // State of request is asynchronous.
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);

            // Read the response into a Stream object.
            Stream responseStream = response.GetResponseStream();

            string data;
            using (var reader = new StreamReader(responseStream))
            {
                data = reader.ReadToEnd();
            }
            JObject obj = JObject.Parse(data);
            finalCallback(obj);
        }

        public static List<String> getBlockList(JObject obj)
        {
            try
            {
                if ((obj == null) || "fail" == (string)obj["stat"])
                {

                    logger.Info("HTTP", "Unable to upload address book");
                    // TODO raise a real exception here
                    return null;
                }
                logger.Info("AccountUtils", "Reply from addressbook:" + obj.ToString());
                JArray blocklist = (JArray)obj["blocklist"];
                if (blocklist == null)
                {
                    logger.Info("AccountUtils", "Received blocklist as null");
                    return null;
                }
                List<String> blockListMsisdns = new List<string>();
                foreach (string num in blocklist)
                {
                    blockListMsisdns.Add(num);
                }
                return blockListMsisdns;
            }
            catch (ArgumentException e)
            {
                logger.Info("AccountUtils", "Improper Argument is passed to the function. Exception : "+e.ToString());
                return null;
            }
            catch (Exception e)
            {
                logger.Info("AccountUtils", "Exception while processing GETBLOCKLIST function : " + e.ToString());
                return null;
            }
        }

        private static JObject getJsonContactList(Dictionary<String, List<ContactInfo>> contactsMap)
        {
            JObject updateContacts = new JObject();
            foreach (String id in contactsMap.Keys)
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

        public static List<ContactInfo> getContactList(JObject obj, Dictionary<String, List<ContactInfo>> new_contacts_by_id)
        {
            try
            {
                if ((obj == null) || "fail" == (string)obj["stat"])
                {

                    logger.Info("HTTP", "Unable to upload address book");
                    // TODO raise a real exception here
                    return null;
                }
                JObject addressbook = (JObject)obj["addressbook"];
                if (addressbook == null)
                {
                    logger.Info("AccountUtils","Invalid JSON object. Addressbook empty.");
                    return null;
                }
                
                List<ContactInfo> server_contacts = new List<ContactInfo>();
                return server_contacts;
            }
            catch (ArgumentException)
            {
                logger.Info("AccountUtils", "Improper Argument is passed to the function.");
                return null;
            }
            catch (Exception e)
            {
                logger.Info("AccountUtils", "Exception while processing GETCONTACTLIST function : "+e.ToString());
                return null;
            }
        }
    }
}
