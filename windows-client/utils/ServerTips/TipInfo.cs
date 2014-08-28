using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using windows_client.utils;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using windows_client.Model;
using System.IO;
using windows_client.Misc;

namespace windows_client.utils.ServerTips
{
    class TipInfo
    {

        public string HeaderText { get; set; }
        public string BodyText { get; set; }
        public string TipId { get; set; }
        public ToolTipMode TipType;

        public TipInfo()
        {
            TipType = ToolTipMode.DEFAULT;
            HeaderText = null;
            BodyText = null;
            TipId = null;
        }

        public TipInfo(string type, string header, string body, string id)
        {
            HeaderText = header;
            BodyText = body;
            TipId = id;

            switch (type)
            {

                case HikeConstants.ServerTips.STICKER_TIPS:

                    TipType = ToolTipMode.STICKERS;
                    break;

                case HikeConstants.ServerTips.PROFILE_TIPS:

                    TipType = ToolTipMode.PROFILE_PIC;
                    break;

                case HikeConstants.ServerTips.ATTACHMENT_TIPS:

                    TipType = ToolTipMode.ATTACHMENTS;
                    break;

                case HikeConstants.ServerTips.INFORMATIONAL_TIPS:

                    TipType = ToolTipMode.INFORMATIONAL;
                    break;
                case HikeConstants.ServerTips.FAVOURITE_TIPS:

                    TipType = ToolTipMode.FAVOURITES;
                    break;

                case HikeConstants.ServerTips.THEME_TIPS:

                    TipType = ToolTipMode.CHAT_THEMES;
                    break;

                case HikeConstants.ServerTips.INVITATION_TIPS:

                    TipType = ToolTipMode.INVITE_FRIENDS;
                    break;

                case HikeConstants.ServerTips.STATUS_UPDATE_TIPS:

                    TipType = ToolTipMode.STATUS_UPDATE;
                    break;

                default:

                    TipType = ToolTipMode.DEFAULT;
                    break;

            };

        }

        public bool IsChatScreenTip
        {
            get
            {
                if (TipType == ToolTipMode.CHAT_THEMES || TipType == ToolTipMode.ATTACHMENTS || TipType == ToolTipMode.STICKERS)
                    return true; //chat thread page
                else
                    return false; //conv page
            }
        }


        #region FILE READ WRITE
        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                TipId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                count = reader.ReadInt32();
                HeaderText = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (HeaderText == "*@N@*")
                    HeaderText = null;

                count = reader.ReadInt32();
                BodyText = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (BodyText == "*@N@*")
                    BodyText = null;

                count = reader.ReadInt32();
                TipType = (ToolTipMode)count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("utils:ServerTips:TipInfo :: Read : Read, Exception : " + ex.StackTrace);
            }
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(TipId);

                if (HeaderText == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(HeaderText);

                if (BodyText == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(BodyText);

                writer.Write((int)TipType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("utils:ServerTips:TipInfo  :: Write: Write Tip To File, Exception : " + ex.StackTrace);
            }
        }
        #endregion
    }
}
