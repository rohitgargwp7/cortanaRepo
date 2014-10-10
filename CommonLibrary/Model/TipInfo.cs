using System;
using System.Text;
using CommonLibrary.Model;
using System.IO;
using CommonLibrary.Lib;

namespace CommonLibrary.Model
{
    class TipInfo : IBinarySerializable
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

        public TipInfo(ToolTipMode type, string header, string body, string id)
        {
            HeaderText = header;
            BodyText = body;
            TipId = id;
            TipType = type;
        }

        public bool IsChatScreenTip
        {
            get
            {
                return (TipType == ToolTipMode.CHAT_THEMES || TipType == ToolTipMode.ATTACHMENTS || TipType == ToolTipMode.STICKERS);
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
