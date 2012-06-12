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
        public static void setIcon(string msisdn, byte[] image)
        {
            //Binary binaryImage = new Binary(image);
            Thumbnails thumbnail = getThumbNailForMSisdn(msisdn);
            if (thumbnail == null)
            {
                App.HikeDataContext.thumbnails.InsertOnSubmit(new Thumbnails(msisdn, image));
                ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                contact.HasCustomPhoto = true;
            }
            else
            {
                thumbnail.Avatar = image;
            }
            App.HikeDataContext.SubmitChanges();
        }

        public static List<Thumbnails> getAllThumbNails()
        {
            Func<HikeDataContext, IQueryable<Thumbnails>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Thumbnails>>
            ((HikeDataContext hdc) =>
                from o in hdc.thumbnails
                select o);
            return q(App.HikeDataContext).Count<Thumbnails>() == 0 ? null :
                q(App.HikeDataContext).ToList<Thumbnails>();
        }


        public static Thumbnails getThumbNailForMSisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<Thumbnails>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<Thumbnails>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.thumbnails
                where o.Msisdn == m
                select o);
            return q(App.HikeDataContext, msisdn).Count<Thumbnails>() == 0 ? null :
                q(App.HikeDataContext, msisdn).First<Thumbnails>();
        }
    }
}
