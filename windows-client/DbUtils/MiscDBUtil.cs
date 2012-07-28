using windows_client.Model;
using System.Linq;
using System.Collections.Generic;

namespace windows_client.DbUtils
{
    public class MiscDBUtil
    {
        public static void clearDatabase()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<Conversation>(context.GetTable<Conversation>());
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                context.SubmitChanges();
            }
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                context.SubmitChanges();
            }
            using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
            {
                context.mqttMessages.DeleteAllOnSubmit<HikePacket>(context.GetTable<HikePacket>());
                context.SubmitChanges();
            }
        }
        
        public static void addOrUpdateIcon(string msisdn, byte[] image)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                ContactInfo cInfo = DbCompiledQueries.GetContactFromMsisdn(context, msisdn).FirstOrDefault<ContactInfo>();
                if (cInfo == null)
                    return;
                if (cInfo.Avatar != image)
                {
                    cInfo.Avatar = image;
                    cInfo.HasCustomPhoto = true;
                    context.SubmitChanges();
                }
            }
        }

       
        public static byte [] getThumbNailForMSisdn(string msisdn)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                ContactInfo cInfo = DbCompiledQueries.GetContactFromMsisdn(context,msisdn).FirstOrDefault<ContactInfo>();
                return (cInfo == null ? null : cInfo.Avatar);
            }
        }

  
    }
}
