using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using windows_client.DbUtils;
using Newtonsoft.Json.Linq;
using System.IO;
using windows_client.Misc;
using windows_client.utils;

namespace windows_client.FileTransfers
{
    public class FileInfo
    {
        public double PercentageTransfer
        {
            get 
            {
                return ((double)BytesTransferred / TotalBytes) * 100;
            }
        }

        public int TotalBytes
        {
            get
            {
                return FileBytes.Length;
            }
        }

        public string Id;
        public int BytesTransferred;
        public int CurrentHeaderPosition;
        public byte[] FileBytes;
        public string ContentType;
        public string FileName;
        public JObject SuccessObj;
        public string FileKey;
        public string Message;

        public Attachment.AttachmentState FileState;

        public ConvMessage ConvMessage;

        public FileInfo()
        {
        }

        public FileInfo(ConvMessage convMessage, byte[] fileBytes)
        {
            ConvMessage = convMessage;
            FileBytes = fileBytes;
            ContentType = convMessage.FileAttachment.ContentType;
            FileName = ConvMessage.FileAttachment.FileName;

            Id = ConvMessage.Msisdn + "___" + ConvMessage.MessageId;

            if (fileBytes != null)
                FileBytes = fileBytes;
            else
                InitFileBytes();
        }

        void InitFileBytes()
        {
            if (ConvMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                FileBytes = Encoding.UTF8.GetBytes(ConvMessage.MetaDataString);
            else
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + ConvMessage.Msisdn + "/" + Convert.ToString(ConvMessage.MessageId), out FileBytes);
        }

        public void Write(BinaryWriter writer)
        {
            if (Id == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Id);

            writer.Write(BytesTransferred);
            writer.Write(CurrentHeaderPosition);

            if (SuccessObj == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(SuccessObj.ToString(Newtonsoft.Json.Formatting.None));

            if (Message == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Message);

            if (FileKey == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(FileKey);

            if (FileName == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(FileName);

            if (ContentType == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(ContentType);

            writer.Write((int)FileState);

            writer.Write(FileBytes != null ? FileBytes.Length : 0);
            if (FileBytes != null)
                writer.Write(FileBytes);
        }

        public void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Id == "*@N@*")
                Id = null;
            
            BytesTransferred = reader.ReadInt32();
            CurrentHeaderPosition = reader.ReadInt32();
            
            count = reader.ReadInt32();
            var str = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (str == "*@N@*")
                SuccessObj = null;
            else
                SuccessObj = JObject.Parse(str);
            
            count = reader.ReadInt32();
            Message = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Message == "*@N@*")
                Message = null;
            
            count = reader.ReadInt32();
            FileKey = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (FileKey == "*@N@*")
                FileKey = null; 

            count = reader.ReadInt32();
            FileName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (FileName == "*@N@*")
                FileName = null;

            count = reader.ReadInt32();
            ContentType = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (ContentType == "*@N@*")
                ContentType = null;

            FileState = (Attachment.AttachmentState)reader.ReadInt32();
            
            count = reader.ReadInt32();
            FileBytes = count != 0 ? reader.ReadBytes(count) : FileBytes = null;
        }
    }
}
