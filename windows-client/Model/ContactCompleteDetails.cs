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
        private string firstName;
        private string middleName;
        private string lastName;
        private string mobile;
        private string telephone;
        private string email;
        private string company;
        private string jobTitle;

        public JObject SerialiseToJobject()
        {
            JObject jobject = new JObject();

            if (!string.IsNullOrEmpty(firstName))
                jobject[HikeConstants.FIRSTNAME] = firstName;

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

            if (!string.IsNullOrEmpty(firstName))
                saveContactTask.FirstName = firstName;

            if (!string.IsNullOrEmpty(middleName))
                saveContactTask.MiddleName = middleName;

            if (!string.IsNullOrEmpty(lastName))
                saveContactTask.LastName = lastName;

            if (!string.IsNullOrEmpty(mobile))
                saveContactTask.MobilePhone = mobile;

            if (!string.IsNullOrEmpty(telephone))
                saveContactTask.HomePhone = telephone;

            if (!string.IsNullOrEmpty(email))
                saveContactTask.WorkEmail = email;

            if (!string.IsNullOrEmpty(company))
                saveContactTask.Company = company;

            if (!string.IsNullOrEmpty(jobTitle))
                saveContactTask.JobTitle = jobTitle;
            return saveContactTask;
        }
        public static ContactCompleteDetails GetContactDetails(Contact c)
        {
            ContactCompleteDetails con = new ContactCompleteDetails();

            con.firstName = c.CompleteName.FirstName;
            con.middleName = c.CompleteName.MiddleName;
            con.lastName = c.CompleteName.LastName;

            foreach (ContactPhoneNumber ph in c.PhoneNumbers)
            {
                if (ph.Kind == PhoneNumberKind.Mobile)
                {
                    con.mobile = ph.PhoneNumber;
                }
                if (ph.Kind == PhoneNumberKind.Home)
                {
                    con.telephone = ph.PhoneNumber;
                }
            }

            foreach (ContactEmailAddress email in c.EmailAddresses)
            {
                if (email.Kind == EmailAddressKind.Work)
                {
                    con.email = email.EmailAddress;
                    break;
                }
            }
            foreach (ContactCompanyInformation compInfo in c.Companies)
            {
                con.company = compInfo.CompanyName;
                con.jobTitle = compInfo.JobTitle;
                break;
            }

            return con;
        }

        public static ContactCompleteDetails GetContactDetails(JObject jsonOnj)
        {
            ContactCompleteDetails con = new ContactCompleteDetails();
            JToken jt;
            jsonOnj.TryGetValue(HikeConstants.FIRSTNAME, out jt);
            if (jt != null)
                con.firstName = jt.ToString();
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
