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
        List<ContactInfo> listFamilyMembers;
        List<ContactInfo> listCloseFriends;
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
                listCloseFriends = new List<ContactInfo>();
                listFamilyMembers = new List<ContactInfo>();

                progressBar.Opacity = 1;
                progressBar.IsEnabled = true;

                if (!(App.appSettings.TryGetValue(HikeConstants.PHONE_ADDRESS_BOOK, out listContactInfo) && listContactInfo != null && listContactInfo.Count > 0))
                {
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                    NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
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
            if (listCloseFriends != null && listCloseFriends.Count > 1)
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
                List<ContactInfo> listContactsFromDb = UsersTableUtils.getAllContacts();
                if (listContactsFromDb == null)
                    listContactsFromDb = new List<ContactInfo>();

                string lastName = GetLastName();
                bool isLastNameCheckApplicable = lastName != null;
                listContact.Sort(new ContactCompare());
                foreach (ContactInfo cn in listContact)
                {
                    int index = listContactsFromDb.IndexOf(cn);
                    if (index < 0)
                    {
                        continue;
                    }
                    ContactInfo contactFromDb = listContactsFromDb[index];

                    cn.Msisdn = contactFromDb.Msisdn;
                    if (!contactFromDb.OnHike)
                    {
                        bool markedForNux = false;
                        if (listFamilyMembers.Count < 31)
                        {
                            if (isLastNameCheckApplicable)
                            {
                                if (!string.IsNullOrEmpty(cn.Name))
                                {
                                    string[] nameArray = cn.Name.Trim().Split(' ');
                                    if (nameArray.Length > 1)
                                    {
                                        string curlastName = nameArray[nameArray.Length - 1].ToLower();
                                        if (curlastName.Trim().ToLower() == lastName)
                                        {
                                            listFamilyMembers.Add(cn);
                                            markedForNux = true;
                                        }
                                    }
                                }
                            }
                            if (!markedForNux && MatchFromFamilyVocab(cn.Name))
                            {
                                markedForNux = true;
                                listFamilyMembers.Add(cn);
                            }
                        }

                        if (!markedForNux && cn.NuxMatchScore > 0)
                        {
                            markedForNux = true;
                            listCloseFriends.Add(cn);
                        }

                    }
                }

                if (listCloseFriends.Count < 31)
                {
                    int contactAdded = 0;
                    int countRequired = 30 - listCloseFriends.Count;
                    foreach (ContactInfo contact in listContact)
                    {
                        int index = listContactsFromDb.IndexOf(contact);
                        if (index < 0)
                        {
                            continue;
                        }
                        if (contactAdded == countRequired)
                            break;
                        ContactInfo contactFromDb = listContactsFromDb[index];

                        contact.Msisdn = contactFromDb.Msisdn;
                        if (!contactFromDb.OnHike && !listCloseFriends.Contains(contact) && !listFamilyMembers.Contains(contact))
                        {
                            listCloseFriends.Add(contact);
                            contactAdded++;
                        }
                    }
                }
                else
                {
                    listCloseFriends.RemoveRange(30, listCloseFriends.Count - 30);
                }
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

        private static string[] familyVocab = new string[] { "aunt", "aunty", "auntie", "uncle", "grandma", "granny", "grandpa", "nanna", "cousin", "‘opà", "aayi", "abatyse", "abba", "abba", "abbi", "aboji", "abonim", "ahm", "äiti", "ama", "amai", "amca", "amma", "ammee", "ammi", "ana", "anne", "anneanne", "anya", "apa", "appa", "apu", "athair", "atta", "aunt", "auntie", "aunty", "ayah", "baabaa", "baba", "babba", "babbo", "banketi", "bapa", "bata", " dai", "bebe", "beta", "beti", "bhabhi", "bhai", "bhaiya", "biang", "bro", "buwa", "chacha", "chachu", "dad", "dada", "daddy", "dadi", "daidí", "daya", "dayı", "dede", "didi", "eadni", "édesapa", "eje", "ema", "emä", "emak", "emo", "ewe", "far", "father", "foter", "fu", "grandma", "grandpa", "haakoro", "haakui", "haha", "ibu", "iloy", "inahan", "induk", "isa", "isä", "itay", "janak", "kantaäiti", "kardeş-im", "kızım", "kohake", "kuzen", "ma", "maa", "macii", "madar", "madèr", "màder", "madr", "mädra", "madre", "mãe", "mai", "maica", "maire", "maji", "majka", "makuahine", "mam", "mama", "mamá", "maman", "mami", "mamm'", "mamm", "mamma", "mamu", "mána", "màna", "mare", "mari", "mat'", "mataji", "mater", "máthair", "mati", "máti", "matka", "matre, mamma", "matri", "me", "mèder", "medra", "mëmë", "mére", "mère", "moæ", "moder", "móðir", "moeder", "moer", "mojer", "mom", "mommy", "mor", "morsa", "mother", "motina", "mueter", "mum", "mummy", "mumsy", "muter", "mutter", "mutti", "mytyr", "mzaa", "mzazi", "nai", "nana", "nanay", "nani", "nay", "nënë", "ñuke", "ñuque", "nyokap", "ôèe", "oğlum", "ojciec", "okaasan", "omm", "oppa", "otac", "otec", "otosan", "pabo", "pai", "pak", "panjo", "papa", "papá", "papà", "papi", "pappa", "pappie", "pare", "parinte", "pater", "patri", "patrino", "pedar", "pita-ji", "pitaji", "pitar", "pop", "popà", "poppa", "pops", "pradininkas", "protevis", "pupà", "reny", "salentino", "sis", "tad", "taica", "tata", "táta", "tàtah", "tatay", "tateh", "tatti", "tay", "tevas", "tevs", "teyze", "uma", "uncle", "vader", "valide", "vieja", "viejo", "yebba", "yeğen", "yenge", "badima", "memaw", "meemaw", "妈", "妈妈", "老妈", "老公", "宝贝", "老婆", "宝贝", "爸", "爸爸", "老爸", "女儿", "闺女", "儿子", "哥", "哥哥", "弟", "弟弟", "姐", "姐姐", "妹", "妹妹", "祖母", "奶奶", "大姨", "小姨", "姑姑", "舅舅", "大舅", "小舅", "叔叔", "伯伯", "表姐", "表妹", "表哥", "表弟", "侄子", "侄女", "uncle", "mama", "mamu", "chacha", "chachu", "mom", "dad", "bhai", "bhaiya", "didi" };

        #endregion

        public static bool MatchFromFamilyVocab(string completeName)
        {
            if (string.IsNullOrEmpty(completeName))
                return false;

            foreach (string vocabKey in familyVocab)
            {
                if (completeName.ToLower().Contains(vocabKey))
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