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
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using System.Threading;

namespace windows_client.View
{
    public partial class ConversationPage : PhoneApplicationPage
    {
        private List<ConvMessage> messages;
        private string msisdn;
        private bool onHike;

        public ConversationPage()
        {
            InitializeComponent();
            this.DataContext = App.ViewModel;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            IDictionary<string, string> parameters = this.NavigationContext.QueryString;
            if (parameters.ContainsKey("Index"))
            {
                int selectedIndex = Convert.ToInt32(NavigationContext.QueryString["Index"]);
                this.DataContext = App.ViewModel.MessageListPageCollection[selectedIndex];
            }
        }

        //loads pre-existing messages for msisdn, returns null if empty
        //sets on-hike status
        private void setConversationPage(string msisdn)
        {
            List<ConvMessage> messages = HikeDbUtils.getMessagesForMsisdn(msisdn);
            onHike = HikeDbUtils.getContactInfoFromMSISDN(msisdn).OnHike;
        }

        //adds an entry for message & an entry in conversation table if it is the first message of the chat thread
        private void addMessage(string message, string msisdn)
        {
            if (messages.Count == 0)
            {
                HikeDbUtils.addConversation(msisdn, onHike);
                //TO discusse
                // add message to list of existing messages and write to db when user quits this page
            }

            ConvMessage convMessage = new ConvMessage(message, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            HikeDbUtils.addMessage(convMessage);
            messages.Add(convMessage);
        }

        private void deleteConversation(string msisdn)
        {
            HikeDbUtils.deleteConversation(msisdn);
        }
    }
}