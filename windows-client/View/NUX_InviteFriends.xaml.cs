using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using System.Diagnostics;
using windows_client.Controls;
using windows_client.DbUtils;
using System.Threading;
using System.ComponentModel;
using Microsoft.Phone.UserData;

namespace windows_client.View
{
    public partial class NUX_InviteFriends : PhoneApplicationPage
    {
        private ApplicationBarIconButton sendInviteIconButton;
        private ApplicationBar appBar;
        private ApplicationBarIconButton skipInviteIconButton;
        private bool isFirstLaunch = true;
        private static List<ContactInfo> listContactInfo;
        private List<ContactInfo> listFamilyMembers;
        private List<ContactInfo> listCloseFriends;
        public NUX_InviteFriends()
        {
            InitializeComponent();

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            sendInviteIconButton = new ApplicationBarIconButton();
            sendInviteIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            sendInviteIconButton.Text = "Invite";
            sendInviteIconButton.IsEnabled = false;
            appBar.Buttons.Add(sendInviteIconButton);
            this.ApplicationBar = appBar;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (isFirstLaunch)
            {
                App.PageState ps;
                if (App.appSettings.TryGetValue(App.PAGE_STATE, out ps) && ps == App.PageState.NUX_SCREEN_FAMILY)
                {
                    txtHeader.Text = AppResources.Nux_YourFamily_Txt;
                    txtConnectHike.Text = AppResources.Nux_FamilyMembersConnect_txt;
                }

                listCloseFriends = new List<ContactInfo>();
                listFamilyMembers = new List<ContactInfo>();

                progressBar.Opacity = 1;
                progressBar.IsEnabled = true;
                listContactInfo = UsersTableUtils.GetContactsFromFile();
                if (listContactInfo == null || listContactInfo.Count == 0)
                {
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                    NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
                    return;
                }
                //upgrade or staging so can skip 
                if (App.appSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE) || !AccountUtils.IsProd)
                {
                    skipInviteIconButton = new ApplicationBarIconButton();
                    skipInviteIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
                    skipInviteIconButton.Text = "Skip";
                    skipInviteIconButton.Click += btnSkipNux_Click;
                    appBar.Buttons.Add(skipInviteIconButton);
                }

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (s, a) =>
                {
                    ProcessNuxContacts(listContactInfo);
                };
                bw.RunWorkerAsync();
                bw.RunWorkerCompleted += LoadingCompleted;

                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();

                isFirstLaunch = false;
            }
        }

        //will run on ui thread
        private void LoadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            App.PageState ps;
            App.appSettings.TryGetValue(App.PAGE_STATE, out ps);
            if (listCloseFriends != null && listCloseFriends.Count > 1 && ps == App.PageState.NUX_SCREEN_FRIENDS)
            {
                listContactInfo = listCloseFriends;
                MarkDefaultChecked();
                lstBoxInvite.ItemsSource = listContactInfo;
                sendInviteIconButton.Click += btnInviteFriends_Click;
            }
            else if (listFamilyMembers != null && listFamilyMembers.Count > 1)
            {
                InitialiseFamilyScreen();
            }
            else
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
            }
            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;

            sendInviteIconButton.IsEnabled = true;
        }

        private void InitialiseFamilyScreen()
        {
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.NUX_SCREEN_FAMILY);
            listContactInfo = listFamilyMembers;
            MarkDefaultChecked();
            lstBoxInvite.ItemsSource = listContactInfo;
            txtHeader.Text = AppResources.Nux_YourFamily_Txt;
            txtConnectHike.Text = AppResources.Nux_FamilyMembersConnect_txt;
            sendInviteIconButton.IsEnabled = true;
            sendInviteIconButton.Click += btnInviteFamily_Click;
        }

        private void MarkDefaultChecked()
        {
            if (listContactInfo != null)
            {
                for (int i = 0; i < listContactInfo.Count; i++)
                {
                    if (i == 15)
                        break;
                    ContactInfo cn = listContactInfo[i];
                    cn.IsCloseFriendNux = true;
                }
            }
        }

        private void btnInviteFriends_Click(object sender, EventArgs e)
        {
            sendInviteIconButton.IsEnabled = false;

            string inviteToken = "";
            int count = 0;
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;
            foreach (ContactInfo cinfo in listContactInfo)
            {
                if (cinfo.IsCloseFriendNux)
                {
                    JObject obj = new JObject();
                    JObject data = new JObject();
                    data[HikeConstants.SMS_MESSAGE] = string.Format(AppResources.sms_invite_message, inviteToken);
                    data[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp();
                    data[HikeConstants.MESSAGE_ID] = -1;
                    obj[HikeConstants.TO] = Utils.NormalizeNumber(cinfo.Msisdn);
                    obj[HikeConstants.DATA] = data;
                    obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                    App.MqttManagerInstance.mqttPublishToServer(obj);
                    count++;
                }
            }
            if (count > 0)
            {
                MessageBox.Show(string.Format(AppResources.InviteUsers_TotalInvitesSent_Txt, count), AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
            }
            if (listFamilyMembers != null && listFamilyMembers.Count > 1)
            {
                InitialiseFamilyScreen();
            }
            else
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
            }
            sendInviteIconButton.Click -= btnInviteFriends_Click;
        }

        private void btnInviteFamily_Click(object sender, EventArgs e)
        {
            sendInviteIconButton.IsEnabled = false;
            string inviteToken = "";
            int count = 0;

            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            foreach (ContactInfo cinfo in listContactInfo)
            {
                if (cinfo.IsCloseFriendNux)
                {
                    JObject obj = new JObject();
                    JObject data = new JObject();
                    data[HikeConstants.SMS_MESSAGE] = string.Format(AppResources.sms_invite_message, inviteToken);
                    data[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp();
                    data[HikeConstants.MESSAGE_ID] = -1;
                    obj[HikeConstants.TO] = Utils.NormalizeNumber(cinfo.Msisdn);
                    obj[HikeConstants.DATA] = data;
                    obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                    App.MqttManagerInstance.mqttPublishToServer(obj);
                    count++;
                }
            }
            if (count > 0)
            {
                MessageBox.Show(string.Format(AppResources.InviteUsers_TotalInvitesSent_Txt, count), AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
            }

            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
            NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
        }

        private void btnSkipNux_Click(object sender, EventArgs e)
        {
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
            NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
        }

        public void ProcessNuxContacts(List<ContactInfo> listContact)
        {
            if (listContact != null && listContact.Count > 0 && listFamilyMembers != null && listCloseFriends != null)
            {
                Stopwatch st = Stopwatch.StartNew();
                List<ContactInfo> listContactsFromDb = UsersTableUtils.getAllContactsToInvite();
                st.Stop();
                Debug.WriteLine("Time taken to fetch contacts to be invited :{0}", st.ElapsedMilliseconds);

                if (listContactsFromDb == null)
                    listContactsFromDb = new List<ContactInfo>();

                string lastName = GetLastName();
                bool isLastNameCheckApplicable = lastName != null;
                st.Reset();
                st.Start();
                listContact.Sort(new ContactCompare());
                st.Stop();
                Debug.WriteLine("Time taken to sort :{0}", st.ElapsedMilliseconds);
                st.Reset();
                st.Start();
                Dictionary<string, ContactInfo> dictContactsInDb = new Dictionary<string, ContactInfo>();
                foreach (ContactInfo cinfo in listContactsFromDb)
                {
                    dictContactsInDb[cinfo.Name + cinfo.PhoneNo] = cinfo;
                }
                foreach (ContactInfo cn in listContact)
                {
                    ContactInfo contactFromDb;
                    if (!dictContactsInDb.TryGetValue(cn.Name + cn.PhoneNo, out contactFromDb))
                        continue;
                    cn.Msisdn = contactFromDb.Msisdn;
                    bool markedForNux = false;
                    if (listFamilyMembers.Count < 31)
                    {
                        if (!string.IsNullOrEmpty(cn.Name))
                        {
                            string[] nameArray = cn.Name.Trim().Split(' ');
                            if (isLastNameCheckApplicable)
                            {
                                if (nameArray.Length > 1)
                                {
                                    string curlastName = nameArray[nameArray.Length - 1];
                                    if (curlastName.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        listFamilyMembers.Add(cn);
                                        markedForNux = true;
                                    }
                                }
                            }
                            if (!markedForNux && MatchFromFamilyVocab(nameArray))
                            {
                                markedForNux = true;
                                listFamilyMembers.Add(cn);
                            }
                        }
                    }

                    if (!markedForNux && cn.NuxMatchScore > 0 && listCloseFriends.Count < 31)
                    {
                        markedForNux = true;
                        listCloseFriends.Add(cn);
                    }

                }
                if (listCloseFriends.Count < 31)
                {
                    int contactAdded = 0;
                    int countRequired = 30 - listCloseFriends.Count;
                    foreach (ContactInfo contact in listContact)
                    {
                        ContactInfo contactFromDb;
                        if (!dictContactsInDb.TryGetValue(contact.Name + contact.PhoneNo, out contactFromDb))
                            continue;
                        if (contactAdded == countRequired)
                            break;
                        contact.Msisdn = contactFromDb.Msisdn;
                        if (!contactFromDb.OnHike && !listCloseFriends.Contains(contact) && !listFamilyMembers.Contains(contact))
                        {
                            listCloseFriends.Add(contact);
                            contactAdded++;
                        }
                    }
                }
                st.Stop();
                Debug.WriteLine("Time fr nux scanning " + st.ElapsedMilliseconds);
            }
        }

        public static string GetLastName()
        {
            string name;
            App.appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name == null)
                return null;

            string[] nameArray = name.Trim().Split(' ');
            if (nameArray.Length == 1)
                return null;

            return nameArray[nameArray.Length - 1].ToLower();
        }

        #region FAMILY VOCABULARY

        private Dictionary<string, bool> dictFamilyVocab = new Dictionary<string, bool> { { "aunt", true }, { "aunty", true }, { "auntie", true }, { "uncle", true }, { "grandma", true }, { "granny", true }, { "grandpa", true }, { "nanna", true }, { "cousin", true }, { "‘opà", true }, { "aayi", true }, { "abatyse", true }, { "abba", true }, { "abbi", true }, { "aboji", true }, { "abonim", true }, { "ahm", true }, { "äiti", true }, { "ama", true }, { "amai", true }, { "amca", true }, { "amma", true }, { "ammee", true }, { "ammi", true }, { "ana", true }, { "anne", true }, { "anneanne", true }, { "anya", true }, { "apa", true }, { "appa", true }, { "apu", true }, { "athair", true }, { "atta", true }, { "ayah", true }, { "baabaa", true }, { "baba", true }, { "babba", true }, { "babbo", true }, { "banketi", true }, { "bapa", true }, { "bata", true }, { " dai", true }, { "bebe", true }, { "beta", true }, { "beti", true }, { "bhabhi", true }, { "bhai", true }, { "bhaiya", true }, { "biang", true }, { "bro", true }, { "buwa", true }, { "chacha", true }, { "chachu", true }, { "dad", true }, { "dada", true }, { "daddy", true }, { "dadi", true }, { "daidí", true }, { "daya", true }, { "dayı", true }, { "dede", true }, { "didi", true }, { "eadni", true }, { "édesapa", true }, { "eje", true }, { "ema", true }, { "emä", true }, { "emak", true }, { "emo", true }, { "ewe", true }, { "far", true }, { "father", true }, { "foter", true }, { "fu", true }, { "haakoro", true }, { "haakui", true }, { "haha", true }, { "ibu", true }, { "iloy", true }, { "inahan", true }, { "induk", true }, { "isa", true }, { "isä", true }, { "itay", true }, { "janak", true }, { "kantaäiti", true }, { "kardeş-im", true }, { "kızım", true }, { "kohake", true }, { "kuzen", true }, { "ma", true }, { "maa", true }, { "macii", true }, { "madar", true }, { "madèr", true }, { "màder", true }, { "madr", true }, { "mädra", true }, { "madre", true }, { "mãe", true }, { "mai", true }, { "maica", true }, { "maire", true }, { "maji", true }, { "majka", true }, { "makuahine", true }, { "mam", true }, { "mamá", true }, { "maman", true }, { "mami", true }, { "mamm'", true }, { "mamm", true }, { "mamma", true }, { "mána", true }, { "màna", true }, { "mare", true }, { "mari", true }, { "mat'", true }, { "mataji", true }, { "mater", true }, { "máthair", true }, { "mati", true }, { "máti", true }, { "matka", true }, { "matre", true }, { " mamma", true }, { "matri", true }, { "me", true }, { "mèder", true }, { "medra", true }, { "mëmë", true }, { "mére", true }, { "mère", true }, { "moæ", true }, { "moder", true }, { "móðir", true }, { "moeder", true }, { "moer", true }, { "mojer", true }, { "mom", true }, { "mommy", true }, { "mor", true }, { "morsa", true }, { "mother", true }, { "motina", true }, { "mueter", true }, { "mum", true }, { "mummy", true }, { "mumsy", true }, { "muter", true }, { "mutter", true }, { "mutti", true }, { "mytyr", true }, { "mzaa", true }, { "mzazi", true }, { "nai", true }, { "nana", true }, { "nanay", true }, { "nani", true }, { "nay", true }, { "nënë", true }, { "ñuke", true }, { "ñuque", true }, { "nyokap", true }, { "ôèe", true }, { "oğlum", true }, { "ojciec", true }, { "okaasan", true }, { "omm", true }, { "oppa", true }, { "otac", true }, { "otec", true }, { "otosan", true }, { "pabo", true }, { "pai", true }, { "pak", true }, { "panjo", true }, { "papa", true }, { "papá", true }, { "papà", true }, { "papi", true }, { "pappa", true }, { "pappie", true }, { "pare", true }, { "parinte", true }, { "pater", true }, { "patri", true }, { "patrino", true }, { "pedar", true }, { "pita-ji", true }, { "pitaji", true }, { "pitar", true }, { "pop", true }, { "popà", true }, { "poppa", true }, { "pops", true }, { "pradininkas", true }, { "protevis", true }, { "pupà", true }, { "reny", true }, { "salentino", true }, { "sis", true }, { "tad", true }, { "taica", true }, { "tata", true }, { "táta", true }, { "tàtah", true }, { "tatay", true }, { "tateh", true }, { "tatti", true }, { "tay", true }, { "tevas", true }, { "tevs", true }, { "teyze", true }, { "uma", true }, { "vader", true }, { "valide", true }, { "vieja", true }, { "viejo", true }, { "yebba", true }, { "yeğen", true }, { "yenge", true }, { "badima", true }, { "memaw", true }, { "meemaw", true }, { "mama", true }, { "mamu", true }, { "妈", true }, { "妈妈", true }, { "老妈", true }, { "老公", true }, { "宝贝", true }, { "老婆", true }, { "爸", true }, { "爸爸", true }, { "老爸", true }, { "女儿", true }, { "闺女", true }, { "儿子", true }, { "哥", true }, { "哥哥", true }, { "弟", true }, { "弟弟", true }, { "姐", true }, { "姐姐", true }, { "妹", true }, { "妹妹", true }, { "祖母", true }, { "奶奶", true }, { "大姨", true }, { "小姨", true }, { "姑姑", true }, { "舅舅", true }, { "大舅", true }, { "小舅", true }, { "叔叔", true }, { "伯伯", true }, { "表姐", true }, { "表妹", true }, { "表哥", true }, { "表弟", true }, { "侄子", true }, { "侄女", true } };

        #endregion

        public bool MatchFromFamilyVocab(string[] strCompleteName)
        {
            if (strCompleteName == null)
                return false;

            foreach (string namesplit in strCompleteName)
            {
                if (dictFamilyVocab.ContainsKey(namesplit.ToLower()))
                    return true;
            }
            return false;
        }
        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
        }
    }
}