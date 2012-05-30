using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.Collections.ObjectModel;
using windows_client.utils;

using windows_client.DbUtils;
using windows_client.Model;
using Newtonsoft.Json.Linq;

namespace windows_client
{
    public partial class MessageList : PhoneApplicationPage
    {
        public MessageList()
        {
            InitializeComponent();
           // AccountUtils.registerAccount(null, null, new AccountUtils.postResponseFunction(AccountUtils.registerPostResponse));
            //this.DataContext = App.DbUtils;
           // MyAccountUtils myAccountUtils = new MyAccountUtils();
           //s myAccountUtils.registerAccount(null, null);

//            App.DbUtils.MessageListPageCollection = new ObservableCollection<MessageListPage>();
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("Madhur", "Hello", "3 minutes ago"));
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("ABCD", "Hello3", "5 minutes ago"));
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("sdhj", "Hello2", "39 minutes ago"));
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("dfvdf", "Hello6", "38 minutes ago"));
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("fvvfev", "Hello8", "37 minutes ago"));
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("dvdfvfd", "Hello9", "36 minutes ago"));
//            App.DbUtils.MessageListPageCollection.Add(new MessageListPage("45fdvdv", "Hello023", "63 minutes ago"));

//            List<ContactInfo> list = new List<ContactInfo>();
//            list.Add(new ContactInfo("-1", "9876543210", "Madhur", false, "9876543210", false));
//            list.Add(new ContactInfo("-1", "9876543221", "Madhur1", false, "9876543221", false));
//            list.Add(new ContactInfo("-1", "9876543214", "Madhur2", false, "9876543214", false));
//            list.Add(new ContactInfo("-1", "9876543219", "Madhur3", false, "9876543219", false));
//            list.Add(new ContactInfo("-1", "9876543234", "Madhur4", false, "9876543234", false));
//            list.Add(new ContactInfo("-1", "9876543241", "Madhur5", false, "9876543241", false));

//            //IJSonObject jsonObj = c1.toJSON();

//            ContactInfo c1 = new ContactInfo("-1", "9876541111", "Madhur6", false, "9876541111", false);
//            JObject j = new JObject();
//            j.Add("Madhur", "Garg");
//            j.Add("MyObject", JToken.FromObject(c1));

//            JObject j2 = JObject.Parse(j.ToString());

//            JArray arr = new JArray();
////            arr.Add(JToken.Parse("blocklist"));
//            arr.Add("98");
//            arr.Add("99");
//            arr.Add("12");
//            arr.Add("34");
//            arr.Add("354");
//            arr.Add("87");
//            arr.Add(344);

//            j.Add("MyArray", arr);

//            j.Add("json", j2);
//            JToken json;
//            JToken myArray;
//            JToken myObj;
//            j.TryGetValue("json",out json);
//            j.TryGetValue("MyArray", out myArray);
//            j.TryGetValue("MyObject", out myObj);
//            JToken n;
//            JObject temp = JObject.FromObject(myObj);
//            temp.TryGetValue("Name", out n);
//            App.DbUtils.addContacts(list);

//            ContactInfo c23 = App.DbUtils.getContactInfoFromMSISDN("9876543210");

//            List<ContactInfo> fromDB;// = new List<ContactInfo>();
//            fromDB = App.DbUtils.getAllContacts();

        }

        private void btnGetSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBoxItem selectedItem = this.myListBox.ItemContainerGenerator.ContainerFromItem(this.myListBox.SelectedItem) as ListBoxItem;
            //this.myListBox.SelectedIndex;
            string uri = "/View/ConversationPage.xaml?Index=";
            uri += this.myListBox.SelectedIndex;
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while(NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
    }
}