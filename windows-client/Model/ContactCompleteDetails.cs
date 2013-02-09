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


        public JObject SerialiseToJobject()
        {
            JObject jobject = new JObject();

            if (!string.IsNullOrEmpty(name))
                jobject[HikeConstants.FIRSTNAME] = name;

            if (!string.IsNullOrEmpty(middleName))
                jobject[HikeConstants.MIDDLENAME] = middleName;

            if (!string.IsNullOrEmpty(lastName))
                jobject[HikeConstants.LASTNAME] = lastName;

            if (!string.IsNullOrEmpty(mobile))
                jobject[HikeConstants.MOBILE] = mobile;

            if (!string.IsNullOrEmpty(telephone))
                jobject[HikeConstants.TELEPHONE] = telephone;

            if (!string.IsNullOrEmpty(email))
                jobject[HikeConstants.EMAIL] = email;

            if (!string.IsNullOrEmpty(company))
                jobject[HikeConstants.COMPANY] = company;

            if (!string.IsNullOrEmpty(jobTitle))
                jobject[HikeConstants.JOBTITLE] = jobTitle;
            return jobject;
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
            jsonOnj.TryGetValue(HikeConstants.FIRSTNAME, out jt);
            if (jt != null)
                con.name = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.MIDDLENAME, out jt);
            if (jt != null)
                con.middleName = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.LASTNAME, out jt);
            if (jt != null)
                con.lastName = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.MOBILE, out jt);
            if (jt != null)
                con.mobile = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.TELEPHONE, out jt);
            if (jt != null)
                con.telephone = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.EMAIL, out jt);
            if (jt != null)
                con.email = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.COMPANY, out jt);
            if (jt != null)
                con.company = jt.ToString();
            jsonOnj.TryGetValue(HikeConstants.JOBTITLE, out jt);
            if (jt != null)
                con.jobTitle = jt.ToString();


            return con;
        }
    }
}
