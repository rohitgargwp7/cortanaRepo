using System.Collections.Generic;
using Microsoft.Phone.Controls;
using windows_client.DbUtils;
using windows_client.Model;
using System.Windows.Controls;
using System.ComponentModel;
using windows_client.utils;

namespace windows_client.View
{
    public partial class InviteUsers : PhoneApplicationPage
    {
        private Dictionary<ContactInfo, bool> invitedUsers = new Dictionary<ContactInfo, bool>();
        public List<Utils.Group<ContactInfo>> groupedList = null;

        public InviteUsers()
        {
            InitializeComponent();
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true; 
            bw.DoWork += new DoWorkEventHandler(bw_LoadAllContacts);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
        }

        private void bw_LoadAllContacts(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                List<ContactInfo> allContactsList = UsersTableUtils.getAllContactsToInvite();
                groupedList = getGroupedList(allContactsList);
            }
        }

        /* This function will run on UI Thread */
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                
            }
            else if (e.Error != null)
            {
               
            }
            else
            {
                contactsListBox.ItemsSource = groupedList;
                progressBar.Visibility = System.Windows.Visibility.Collapsed;
                progressBar.IsEnabled = false ;
            }
        }

        private List<Utils.Group<ContactInfo>> getGroupedList(List<ContactInfo> allContactsList)
        {
            if (allContactsList == null || allContactsList.Count == 0)
                return null;
            
            List<Utils.Group<ContactInfo>> glist = createGroups();

           
            for (int i = 0; i < allContactsList.Count; i++)
            {
                ContactInfo c = allContactsList[i];
               
                if (c.Msisdn == App.MSISDN) // don't show own number in any chat.
                    continue;
                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 0 : ch[0] - 'a' + 1;
                // and add the entry
                glist[index].Items.Add(c);
            }
            return glist;
        }

        private List<Utils.Group<ContactInfo>> createGroups()
        {
            string Groups = "#abcdefghijklmnopqrstuvwxyz";
            List<Utils.Group<ContactInfo>> glist = new List<Utils.Group<ContactInfo>>();
            foreach (char c in Groups)
            {
                Utils.Group<ContactInfo> g = new Utils.Group<ContactInfo>(c.ToString(), new List<ContactInfo>());
                glist.Add(g);
            }
            return glist;
        }

        private static string GetCaptionGroup(ContactInfo c)
        {
            char key = char.ToLower(c.Name[0]);
            if (key < 'a' || key > 'z')
            {
                key = '#';
            }
            return key.ToString();
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void inviteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ContactInfo obj = (sender as Button).DataContext as ContactInfo;
            obj.IsInvited = true;
            if (obj == null || obj.OnHike || invitedUsers.ContainsKey(obj))
                return;
            invitedUsers.Add(obj, true);
            long time = utils.TimeUtils.getCurrentTimeStamp();
            ConvMessage convMessage = new ConvMessage(App.invite_message,obj.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsInvite=true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
        }
    }
}