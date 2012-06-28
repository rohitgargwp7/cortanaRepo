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
        public static void addOrUpdateIcon(string msisdn, byte[] image)
        {
            Thumbnails thumbnail = getThumbNailForMSisdn(msisdn);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
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
            Func<HikeDataContext, IQueryable<Thumbnails>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Thumbnails>>
            ((HikeDataContext hdc) =>
                from o in hdc.thumbnails
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context).Count<Thumbnails>() == 0 ? null :
                    q(context).ToList<Thumbnails>();
            }
        }


        public static Thumbnails getThumbNailForMSisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<Thumbnails>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<Thumbnails>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.thumbnails
                where o.Msisdn == m
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, msisdn).Count<Thumbnails>() == 0 ? null :
                    q(context, msisdn).First<Thumbnails>();
            }
        }
    }
}
