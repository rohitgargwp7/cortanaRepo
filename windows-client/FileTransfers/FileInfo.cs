using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using windows_client.DbUtils;
using Newtonsoft.Json.Linq;

namespace windows_client.FileTransfers
{
    public class FileInfo
    {
        public double PercentageTransfer
        {
            get 
            {
                return ((double)BytesTransferred / TotalSize) * 100;
            }
        }

        public int TotalSize
        {
            get
            {
                return FileBytes.Length;
            }
        }

        public string Id
        {
            get
            {
                return Msisdn + MessageId;
            }
        }

        public int BytesTransferred;
        public int CurrentHeaderPosition;
        public byte[] FileBytes;
        public string ContentType;
        public string Msisdn;
        public long MessageId;
        public string MetaDataString;
        public string FileName;
        public JObject SuccessObj;
        public string FileKey;
        public string Message;

        public Attachment.AttachmentState FileState;

        public ConvMessage ConvMessage;

        public FileInfo(ConvMessage convMessage, byte[] fileBytes)
        {
            ConvMessage = convMessage;
            FileBytes = fileBytes;
            MetaDataString = convMessage.MetaDataString;
            ContentType = convMessage.FileAttachment.ContentType;
            Msisdn = ConvMessage.Msisdn;
            MessageId = ConvMessage.MessageId;
            FileName = ConvMessage.FileAttachment.FileName;

            InitFileBytes();
        }

        void InitFileBytes()
        {
            if (ContentType.Contains(HikeConstants.CT_CONTACT))
                FileBytes = Encoding.UTF8.GetBytes(MetaDataString);
            else
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + Msisdn + "/" + Convert.ToString(MessageId), out FileBytes);
        }

        void Write()
        {
        }

        void Read()
        {
        }
    }
}
