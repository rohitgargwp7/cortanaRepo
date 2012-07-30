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
                context.conversations.DeleteAllOnSubmit<ConversationListObject>(context.GetTable<ConversationListObject>());
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                context.SubmitChanges();
            }
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.thumbnails.DeleteAllOnSubmit<Thumbnails>(context.GetTable<Thumbnails>());
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                context.SubmitChanges();
            }
            using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
            {
                context.mqttMessages.DeleteAllOnSubmit<HikePacket>(context.GetTable<HikePacket>());
                context.SubmitChanges();
            }
        }
        
        public static void addOrUpdateProfileIcon(string msisdn, byte[] image)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Thumbnails> res = DbCompiledQueries.GetIconForMsisdn(context, msisdn).ToList<Thumbnails>();
                Thumbnails thumbnail = (res == null || res.Count == 0) ? null : res.First();                  

                if (thumbnail == null)
                {
                    context.thumbnails.InsertOnSubmit(new Thumbnails(msisdn, image));
                }
                else
                {
                    thumbnail.Avatar = image;
                }
                context.SubmitChanges();
            }
        }

        public static void addOrUpdateIcon(string msisdn, byte[] image)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Thumbnails> res = DbCompiledQueries.GetIconForMsisdn(context, msisdn).ToList<Thumbnails>();
                Thumbnails thumbnail = (res == null || res.Count == 0) ? null : res.First();   

                if (thumbnail == null)
                {
                    ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    if (contact == null)
                        return;
                    context.thumbnails.InsertOnSubmit(new Thumbnails(msisdn, image));                    
                    contact.HasCustomPhoto = true;
                }
                else
                {
                    thumbnail.Avatar = image;
                }
                context.SubmitChanges();
            }
        }

        public static List<Thumbnails> getAllThumbNails()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Thumbnails> res = DbCompiledQueries.GetAllIcons(context).ToList<Thumbnails>();
                return (res == null || res.Count == 0) ? null : res;                   
            }
        }

        public static Thumbnails getThumbNailForMSisdn(string msisdn)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Thumbnails> res = DbCompiledQueries.GetIconForMsisdn(context, msisdn).ToList<Thumbnails>();
                return (res == null || res.Count == 0) ? null : res.First();                   
            }
        }

        public static void deleteAllThumbnails()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.thumbnails.DeleteAllOnSubmit<Thumbnails>(context.GetTable<Thumbnails>());
            }
        }

    }
}
