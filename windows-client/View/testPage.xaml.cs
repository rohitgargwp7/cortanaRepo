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
        }
        public ObservableCollection<ConvMessage> ocMessages;
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE))
            {
                object obj = PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE];
                if (obj is ConversationListObject)
                {
                    ConversationListObject cobj = (ConversationListObject)obj;
                    msisdn = cobj.Msisdn;
                    List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(msisdn, long.MaxValue, 20);
                    ocMessages = new ObservableCollection<ConvMessage>(messagesList);
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

            return new DataTemplate();
        }
    }

}