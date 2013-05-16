using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.DbUtils;
using System.Collections.ObjectModel;
using windows_client.Controls;

namespace windows_client.View
{
    public partial class testPage : PhoneApplicationPage
    {
        string msisdn;
        public testPage()
        {
            InitializeComponent();
            instance = this;
        }
        public static testPage instance;
        public ObservableCollection<ConvMessage> ocMessages;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE))
            {
                object obj = PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE];
                if (obj is ConversationListObject)
                {
                    ConversationListObject cobj = (ConversationListObject)obj;
                    msisdn = cobj.Msisdn;
                    List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(msisdn, long.MaxValue, 20);
                    messagesList.Reverse();
                    ocMessages = new ObservableCollection<ConvMessage>(messagesList);
                    testLLS.ItemsSource = ocMessages;
                }
            }
        }
        private void testLLS_ItemRealized(object sender, ItemRealizationEventArgs e)
        {

        }
    }
    public class ChatThreadTemplateSelector : TemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Determine which template to return;
            ConvMessage convMesssage = (ConvMessage)item;
            if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
            {
                if (convMesssage.IsSent)
                    return testPage.instance.SentMessage;
                else
                {
                    if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                        return testPage.instance.dtRecNudge;
                    else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        return testPage.instance.dtRecContact;
                    else if (convMesssage.FileAttachment != null)
                        return testPage.instance.dtRecFile;
                    else
                        return testPage.instance.dtRecText;
                }
            }
            else
                return testPage.instance.Notification;
        }
    }

}