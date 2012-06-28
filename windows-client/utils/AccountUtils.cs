using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
            REGISTER_ACCOUNT, INVITE, VALIDATE_NUMBER, SET_NAME, DELETE_ACCOUNT , POST_ADDRESSBOOK
        }
        private static void addToken(HttpWebRequest req)
        {
            req.Headers["Cookie"] = "user=" + mToken;
        }

        public static void registerAccount(string pin, string unAuthMSISDN, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            //req.Headers["X-MSISDN"] = "919810116420";
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.REGISTER_ACCOUNT, pin, unAuthMSISDN, finalCallbackFunction });            
        }

        public static void postAddressBook(string token, Dictionary<string, List<ContactInfo>> contactListMap, postResponseFunction finalCallbackFunction)
        {

            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/addressbook")) as HttpWebRequest;
            addToken(req);
           // req.Headers["Cookie"] = "user=" + token;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.POST_ADDRESSBOOK, token, contactListMap, finalCallbackFunction });
        }

        public static void invite(string phone_no, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/user/invite")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.INVITE, phone_no, finalCallbackFunction });
        }

        public static void validateNumber(string phoneNo, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/validate")) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.VALIDATE_NUMBER, phoneNo, finalCallbackFunction});
        }

        public static void setName(string name, postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account/name")) as HttpWebRequest;
            addToken(req);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.SET_NAME, name, finalCallbackFunction });
        }

        public static void deleteAccount(postResponseFunction finalCallbackFunction)
        {
            HttpWebRequest req = HttpWebRequest.Create(new Uri(BASE + "/account")) as HttpWebRequest;
            addToken(req);
            req.Method = "DELETE";
            addToken(req);
            req.BeginGetResponse(json_Callback, new object[] { req, RequestType.DELETE_ACCOUNT, finalCallbackFunction });
            //req.BeginGetRequestStream(setParams_Callback, new object[] { req, RequestType.DELETE_ACCOUNT, finalCallbackFunction });
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
                case RequestType.REGISTER_ACCOUNT:
                    string pin = vars[2] as string;
                    string unAuthMSISDN = vars[3] as string;
                    finalCallbackFunction = vars[4] as postResponseFunction;
                    data.Add("set_cookie","0");
                    if (pin != null)
                    {
                        data.Add("msisdn", unAuthMSISDN);
                        data.Add("pin", pin);
                    }
                    break;

                case RequestType.INVITE:
                    string phoneNo = vars[2] as string;                    
                    data.Add("to",phoneNo);
                    break;

                case RequestType.VALIDATE_NUMBER:
                    string numberToValidate = vars[2] as string;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data.Add("phone_no", numberToValidate);
                    break;

                case RequestType.SET_NAME:
                    string nameToSet = vars[2] as string;
                    finalCallbackFunction = vars[3] as postResponseFunction;
                    data.Add("name", nameToSet);
                    break;

                case RequestType.POST_ADDRESSBOOK:
                    string token = vars[2] as string;
                    Dictionary<string, List<ContactInfo>> contactListMap = vars[3] as Dictionary<string, List<ContactInfo>>;
                    finalCallbackFunction = vars[4] as postResponseFunction;
                    data = getJsonContactList(contactListMap);
                    break;

                case RequestType.DELETE_ACCOUNT:
                    finalCallbackFunction = vars[2] as postResponseFunction;
                    break;

                default:
                    break;
            }
            using (StreamWriter sw = new StreamWriter(postStream))
            {
                string json = data.ToString(Newtonsoft.Json.Formatting.None);
                sw.Write(json);
            }
            postStream.Close();
            req.BeginGetResponse(json_Callback, new object[] { req, type, finalCallbackFunction });
        }

        private static void json_Callback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            RequestType type = (RequestType)vars[1];
            postResponseFunction finalCallbackFunction = vars[2] as postResponseFunction;
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            string data;
            JObject obj = null;

            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                Stream responseStream = response.GetResponseStream();
                using (var reader = new StreamReader(responseStream))
                {
                    data = reader.ReadToEnd();
                }
                obj = JObject.Parse(data);
            }
            catch (IOException ioe)
            {
                logger.Info("AccountUtils", "RequestType : " + type + " , IOException occured : " + ioe);
                obj = null;
            }
            catch (WebException we)
            {
                logger.Info("AccountUtils", "RequestType : " + type + " , Webexception occured : " + we);
                obj = null;
            }
            catch (JsonException je)
            {
                logger.Info("AccountUtils", "RequestType : " + type + " , Invalid JSON Response", je);
                obj = null;
            }
            catch (Exception e)
            {
                logger.Info("AccountUtils", "RequestType : " + type + " , Exception occured : " + e);
                obj = null;
            }
            finally
            {
                finalCallbackFunction(obj);
            }
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

                //JToken bl = (JToken)obj["blocklist"];
                JArray blocklist = (JArray)obj["blocklist"];

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
                logger.Info("AccountUtils", "Improper Argument is passed to the function. Exception : "+e);
                return null;
            }
            catch (Exception e)
            {
                logger.Info("AccountUtils", "Exception while processing GETBLOCKLIST function : " + e);
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
