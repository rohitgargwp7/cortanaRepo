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
using System.Data.Linq;
using windows_client.Model;
using System.Linq;
using System.Collections.Generic;

namespace windows_client.DbUtils
{
    public class MiscDBUtil
    {
        public static void clearDatabase()
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<Conversation>(context.GetTable<Conversation>());
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.thumbnails.DeleteAllOnSubmit<Thumbnails>(context.GetTable<Thumbnails>());
                context.mqttMessages.DeleteAllOnSubmit<HikePacket>(context.GetTable<HikePacket>());
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                context.SubmitChanges();
            }
        }
        
        public static void addOrUpdateProfileIcon(string msisdn, byte[] image)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<Thumbnails> res = DbCompiledQueries.GetAllIcons(context).ToList<Thumbnails>();
                return (res == null || res.Count == 0) ? null : res;                   
            }
        }

        public static Thumbnails getThumbNailForMSisdn(string msisdn)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<Thumbnails> res = DbCompiledQueries.GetIconForMsisdn(context, msisdn).ToList<Thumbnails>();
                return (res == null || res.Count == 0) ? null : res.First();                   
            }
        }

        public static void deleteAllThumbnails()
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.thumbnails.DeleteAllOnSubmit<Thumbnails>(context.GetTable<Thumbnails>());
            }
        }

    }
}
