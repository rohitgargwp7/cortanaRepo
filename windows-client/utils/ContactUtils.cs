using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using windows_client.Model;

namespace windows_client.utils
{
    public class ContactUtils
    {
        public void getContacts()
        {
            InputScope scope = new InputScope();
            InputScopeName scopeName = new InputScopeName();
            Contacts cons = new Contacts();
            cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(Contacts_SearchCompleted);
            cons.SearchAsync(String.Empty, FilterKind.None, "State String 1");
        }

        void Contacts_SearchCompleted(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> contactListMap = new Dictionary<string, List<ContactInfo>>();
                int id = 0;
                IEnumerable<Contact> contacts = e.Results;
                foreach (Contact cn in contacts)
                {
                    id++;
                    CompleteName cName = cn.CompleteName;
                    List<ContactInfo> contactList = new List<ContactInfo>();
                    foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                    {
                        ContactInfo cInfo = new ContactInfo(Convert.ToString(id), null,cn.DisplayName,ph.PhoneNumber);
                        contactList.Add(cInfo);
                    }
                }
            }
            catch (System.Exception)
            {
                //That's okay, no results//
            }
        }
    }
}
