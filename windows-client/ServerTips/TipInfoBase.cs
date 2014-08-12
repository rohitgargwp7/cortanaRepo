using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.HikeConstants;
using System.IO;
using windows_client.Controls;
using windows_client.Misc;


namespace windows_client.ServerTips
{
    class TipInfoBase
    {
        public string Type { get; set; }
        public string HeadText { get; set; }
        public string BodyText { get; set; }
        public string TipId { get; set; }

        public TipInfoBase(string type, string header, string body, string id)
        {
            Type = type;
            HeadText = header;
            BodyText = body;
            TipId = id;
        }

        public TipInfoBase()
        {
            Type = null;
            HeadText = null;
            BodyText = null;
            TipId = null;
        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                TipId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                count = reader.ReadInt32();
                HeadText = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (HeadText == "*@N@*")
                    HeadText = null;

                count = reader.ReadInt32();
                BodyText = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (BodyText == "*@N@*")
                    BodyText = null;

                count = reader.ReadInt32();
                Type = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (Type == "*@N@*")
                    Type = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ProTip :: Read : Read, Exception : " + ex.StackTrace);
            }
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(TipId);

                if (HeadText == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(HeadText);

                if (BodyText == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(BodyText);

                if (Type == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Type);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TipInfoBase :: Write: Write Tip To File, Exception : " + ex.StackTrace);
            }
        }
    }
}
