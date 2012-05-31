using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;

namespace windows_client.utils
{
    public class AccountUtils
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //public static readonly string HOST = "ec2-122-248-223-107.ap-southeast-1.compute.amazonaws.com"; 46.137.211.136
        public static readonly string HOST = "46.137.211.136";

        private static readonly int PORT = 3001;

        private static readonly string BASE = "http://" + HOST + ":" + Convert.ToString(PORT) + "/v1";

        public static readonly string NETWORK_PREFS_NAME = "NetworkPrefs";

        private static string mToken = null;

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

        public delegate void postResponseFunction(JObject obj);

        private enum RequestType
        {
            REGISTER_ACCOUNT, INVITE, VALIDATE_NUMBER, SET_NAME, DELETE , POST_ADDRESSBOOK
        }
        private static void addToken(HttpWebRequest req)
        {
            req.Headers["Cookie"] = "user=" + mToken;
        }

        public static void registerAccount(string pin, string unAuthMSISDN, postResponseFunction readonlyCallback)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
           // req.Headers["X-MSISDN"] = "919999711370";
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.REGISTER_ACCOUNT, pin, unAuthMSISDN, readonlyCallback });            
        }

        public static void postAddressBook(string token, Dictionary<string, List<ContactInfo>> contactListMap, postResponseFunction readonlyCallback)
        {

            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/addressbook")) as HttpWebRequest;
            addToken(req);
           // req.Headers["Cookie"] = "user=" + token;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.POST_ADDRESSBOOK, token, contactListMap, readonlyCallback });
        }

        public static void invite(string phone_no)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/user/invite")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.INVITE, phone_no });
        }

        public static void validateNumber(string phoneNo, postResponseFunction readonlyCallback)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/validate")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.VALIDATE_NUMBER, phoneNo, readonlyCallback});
        }

        public static void setName(string name, postResponseFunction readonlyCallback)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/name")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SET_NAME, name, readonlyCallback });
        }

        public static void deleteAccount()
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            addToken(req);
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
            postResponseFunction readonlyCallback = null;
            RequestType type = (RequestType)vars[1];
            switch (type)
            {
                case RequestType.REGISTER_ACCOUNT:
                    string pin = vars[2] as string;
                    string unAuthMSISDN = vars[3] as string;
                    readonlyCallback = vars[4] as postResponseFunction;
                    data.Add("set_cookie","0");
                    if (pin != null)
                    {
                        data.Add("msisdn", unAuthMSISDN);
                        data.Add("pin", pin);
                    }
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        //byte [] requeststring= Encoding.UTF8.GetBytes(json);
                        
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
                    readonlyCallback = vars[3] as postResponseFunction;
                    data.Add("phone_no", numberToValidate);
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        sw.Write(json);
                    }
                    break;

                case RequestType.SET_NAME:
                    string nameToSet = vars[2] as string;
                    readonlyCallback = vars[3] as postResponseFunction;
                    data.Add("name", nameToSet);
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                        sw.Write(json);
                    }
                    break;
                case RequestType.POST_ADDRESSBOOK:
                    string token = vars[2] as string;
                    Dictionary<string, List<ContactInfo>> contactListMap = vars[3] as Dictionary<string, List<ContactInfo>>;
                    readonlyCallback = vars[4] as postResponseFunction;
                    data = getJsonContactList(contactListMap);
                    using (StreamWriter sw = new StreamWriter(postStream))
                    {
                       
                        string json = data.ToString(Newtonsoft.Json.Formatting.None);
                       // string json = "{\"3\":[{\"phone_no\":\"9910000474\",\"name\":\"Vijay\"}],\"2\":[{\"phone_no\":\"9711118690\",\"name\":\"Anuj\"}]}"
                       sw.Write(json);
                        
                    }
                    break;
                default:
                    break;
            }
            postStream.Close();
            req.BeginGetResponse(json_Callback, new object[] { req, type, readonlyCallback });
        }

        private static void json_Callback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            postResponseFunction readonlyCallback = vars[2] as postResponseFunction;
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
            readonlyCallback(obj);
        }

        public static List<string> getBlockList(JObject obj)
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

                JToken bl = (JToken)obj["blocklist"];
                JArray blocklist = JArray.FromObject(bl);

                if (blocklist == null)
                {
                    logger.Info("AccountUtils", "Received blocklist as null");
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
                logger.Info("AccountUtils", "Improper Argument is passed to the function. Exception : "+e.ToString());
                return null;
            }
            catch (Exception e)
            {
                logger.Info("AccountUtils", "Exception while processing GETBLOCKLIST function : " + e.ToString());
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

        public static List<ContactInfo> getContactList(JObject obj, Dictionary<string, List<ContactInfo>> new_contacts_by_id)
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
                IEnumerator<KeyValuePair<string,JToken>> keyVals = addressbook.GetEnumerator();
                KeyValuePair<string, JToken> kv;
                while(keyVals.MoveNext())
                {
                    kv = keyVals.Current;
                    JArray entries = (JArray)addressbook[kv.Key];
                    List<ContactInfo> cList = new_contacts_by_id[kv.Key];
                    for (int i = 0; i < entries.Count; ++i)
                    {
                        JObject entry = (JObject)entries[i];
                        string msisdn = (string)entry["msisdn"];
                        bool onhike = (bool)entry["onhike"];
                        ContactInfo info = new ContactInfo(kv.Key, msisdn, cList[i].Name, onhike, cList[i].PhoneNo);
                        server_contacts.Add(info);
                    }
                }
               
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
