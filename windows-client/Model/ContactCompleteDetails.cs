using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.Model
{
    public class ContactCompleteDetails
    {
        private string name;
        private string homeAddress;
        private string homePhone;
        private string mobileNumber;
        private string personalEmail;
        private string workAddress;
        private string workEmail;
        private string workPhone;
        private string otherEmail;

        public string Name
        {
            get
            {
                return name;
            }
        }

        public JObject SerialiseToJobject()
        {
            JObject metadata = new JObject();
            JArray filesData = new JArray();
            JObject jobject = new JObject();

            if (!string.IsNullOrEmpty(name))
                jobject[HikeConstants.CS_NAME] = name;

            JArray jarray = new JArray();

            if (!string.IsNullOrEmpty(mobileNumber))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_MOBILE_KEY] = mobileNumber;
                jarray.Add(jobj);
            }
            if (!string.IsNullOrEmpty(homePhone))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_HOME_KEY] = homePhone;
                jarray.Add(jobj);
            }
            if (!string.IsNullOrEmpty(workPhone))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_WORK_KEY] = workPhone;
                jarray.Add(jobj);
            }
            if (jarray.Count() > 0)
            {
                jobject[HikeConstants.CS_PHONE_NUMBERS] = jarray;
            }
            jarray = new JArray();
            if (!string.IsNullOrEmpty(personalEmail))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_HOME_KEY] = personalEmail;
                jarray.Add(jobj);
            }

            if (!string.IsNullOrEmpty(workEmail))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_WORK_KEY] = workEmail;
                jarray.Add(jobj);
            }
            if (!string.IsNullOrEmpty(otherEmail))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_OTHERS_KEY] = otherEmail;
                jarray.Add(jobj);
            }
            if (jarray.Count() > 0)
            {
                jobject[HikeConstants.CS_EMAILS] = jarray;
            }

            jarray = new JArray();
            if (!string.IsNullOrEmpty(homeAddress))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_HOME_KEY] = homeAddress;
                jarray.Add(jobj);
            }

            if (!string.IsNullOrEmpty(workAddress))
            {
                JObject jobj = new JObject();
                jobj[HikeConstants.CS_WORK_KEY] = workAddress;
                jarray.Add(jobj);
            }

            if (jarray.Count() > 0)
            {
                jobject[HikeConstants.CS_ADDRESSES] = jarray;
            }

            jobject[HikeConstants.FILE_NAME] = string.IsNullOrEmpty(name) ? "Contact" : name;
            jobject[HikeConstants.FILE_CONTENT_TYPE] = HikeConstants.CT_CONTACT;
            filesData.Add(jobject);

            metadata[HikeConstants.FILES_DATA] = filesData;
            return metadata;
        }

        public SaveContactTask GetSaveCotactTask()
        {
            SaveContactTask saveContactTask = new SaveContactTask();

            if (!string.IsNullOrEmpty(name))
                saveContactTask.FirstName = name;

            if (!string.IsNullOrEmpty(homeAddress))
                saveContactTask.HomeAddressCity = homeAddress;

            if (!string.IsNullOrEmpty(homePhone))
                saveContactTask.HomePhone = homePhone;

            if (!string.IsNullOrEmpty(mobileNumber))
                saveContactTask.MobilePhone = mobileNumber;

            if (!string.IsNullOrEmpty(personalEmail))
                saveContactTask.PersonalEmail = personalEmail;

            if (!string.IsNullOrEmpty(workAddress))
                saveContactTask.WorkAddressCity = workAddress;

            if (!string.IsNullOrEmpty(workEmail))
                saveContactTask.WorkEmail = workEmail;

            if (!string.IsNullOrEmpty(workPhone))
                saveContactTask.WorkPhone = workPhone;

            if (!string.IsNullOrEmpty(otherEmail))
                saveContactTask.OtherEmail = otherEmail;

            return saveContactTask;
        }

        public static ContactCompleteDetails GetContactDetails(Contact c)
        {
            ContactCompleteDetails con = new ContactCompleteDetails();


            if (!string.IsNullOrEmpty(c.CompleteName.FirstName))
                con.name = c.CompleteName.FirstName;
            if (!string.IsNullOrEmpty(c.CompleteName.MiddleName))
                con.name += " " + c.CompleteName.MiddleName;
            if (!string.IsNullOrEmpty(c.CompleteName.LastName))
                con.name += " " + c.CompleteName.LastName;

            foreach (ContactPhoneNumber ph in c.PhoneNumbers)
            {
                if (ph.Kind == PhoneNumberKind.Mobile)
                {
                    con.mobileNumber = ph.PhoneNumber;
                }
                if (ph.Kind == PhoneNumberKind.Home)
                {
                    con.homePhone = ph.PhoneNumber;
                }
                if (ph.Kind == PhoneNumberKind.Work)
                {
                    con.workPhone = ph.PhoneNumber;
                }
            }

            foreach (ContactEmailAddress email in c.EmailAddresses)
            {
                if (email.Kind == EmailAddressKind.Work)
                {
                    con.workEmail = email.EmailAddress;
                }
                if (email.Kind == EmailAddressKind.Personal)
                {
                    con.personalEmail = email.EmailAddress;
                }
                if (email.Kind == EmailAddressKind.Other)
                {
                    con.otherEmail = email.EmailAddress;
                }
            }

            foreach (ContactAddress address in c.Addresses)
            {
                if (address.Kind == AddressKind.Work)
                {
                    con.workAddress = address.PhysicalAddress.AddressLine1;
                }
                if (address.Kind == AddressKind.Home)
                {
                    con.homeAddress = address.PhysicalAddress.AddressLine1;
                }
            }
            return con;
        }

        public static ContactCompleteDetails GetContactDetails(JObject jsonOnj)
        {
            ContactCompleteDetails con = new ContactCompleteDetails();
            JToken jt;
            if (jsonOnj.TryGetValue(HikeConstants.CS_NAME, out jt) && jt != null)
                con.name = jt.ToString();

            KeyValuePair<string, JToken> kv;

            if (jsonOnj.TryGetValue(HikeConstants.CS_PHONE_NUMBERS, out jt) && jt != null && jt is JArray)
            {
                JArray phoneNumbers = (JArray)jt;

                bool isMobileSet = false;
                bool isHomePhoneSet = false;
                bool isWorkSet = false;
                List<string> listUnAssignedNumbers = new List<string>();
                foreach (JObject jobj in phoneNumbers)
                {
                    IEnumerator<KeyValuePair<string, JToken>> keyVals = jobj.GetEnumerator();
                    while (keyVals.MoveNext())
                    {
                        kv = keyVals.Current;

                        if (!isMobileSet && kv.Key.ToLower().Contains(HikeConstants.CS_MOBILE_KEY.ToLower()))
                        {
                            con.mobileNumber = kv.Value.ToString();
                            isMobileSet = true;
                        }
                        else if (!isHomePhoneSet && kv.Key.ToLower().Contains(HikeConstants.CS_HOME_KEY.ToLower()))
                        {
                            con.homePhone = kv.Value.ToString();
                            isHomePhoneSet = true;
                        }
                        else if (!isWorkSet && kv.Key.ToLower().Contains(HikeConstants.CS_WORK_KEY.ToLower()))
                        {
                            con.workPhone = kv.Value.ToString();
                            isWorkSet = true;
                        }
                        else
                        {
                            listUnAssignedNumbers.Add(kv.Value.ToString());
                        }
                    }
                }

                if (listUnAssignedNumbers.Count > 0)
                {
                    int i = 0;
                    if (!isMobileSet && listUnAssignedNumbers.Count > i)
                    {
                        con.mobileNumber = listUnAssignedNumbers[i];
                        i++;
                    }
                    if (!isHomePhoneSet && listUnAssignedNumbers.Count > i)
                    {
                        con.homePhone = listUnAssignedNumbers[i];
                        i++;
                    }
                    if (!isWorkSet && listUnAssignedNumbers.Count > i)
                    {
                        con.workPhone = listUnAssignedNumbers[i];
                    }
                }
            }


            if (jsonOnj.TryGetValue(HikeConstants.CS_EMAILS, out jt) && jt != null && jt is JArray)
            {
                JArray emails = (JArray)jt;

                foreach (JObject jobj in emails)
                {
                    IEnumerator<KeyValuePair<string, JToken>> keyVals = jobj.GetEnumerator();
                    while (keyVals.MoveNext())
                    {
                        kv = keyVals.Current;

                        if (kv.Key.ToLower().Contains(HikeConstants.CS_WORK_KEY.ToLower()))
                            con.workEmail = kv.Value.ToString();

                        if (kv.Key.ToLower().Contains(HikeConstants.CS_HOME_KEY.ToLower()))
                            con.personalEmail = kv.Value.ToString();

                        if (kv.Key.ToLower().Contains(HikeConstants.CS_OTHERS_KEY.ToLower()))
                            con.otherEmail = kv.Value.ToString();
                    }
                }
            }

            if (jsonOnj.TryGetValue(HikeConstants.CS_ADDRESSES, out jt) && jt != null && jt is JArray)
            {
                JArray addressess = (JArray)jt;

                foreach (JObject jobj in addressess)
                {
                    IEnumerator<KeyValuePair<string, JToken>> keyVals = jobj.GetEnumerator();
                    while (keyVals.MoveNext())
                    {
                        kv = keyVals.Current;
                        if (kv.Key.ToLower().Contains(HikeConstants.CS_WORK_KEY.ToLower()))
                            con.workAddress = kv.Value.ToString();

                        if (kv.Key.ToLower().Contains(HikeConstants.CS_HOME_KEY.ToLower()))
                            con.homeAddress = kv.Value.ToString();
                    }
                }
            }
            return con;
        }
    }
}
