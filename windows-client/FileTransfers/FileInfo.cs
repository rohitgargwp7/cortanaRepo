using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using windows_client.DbUtils;

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

        public int BytesTransferred;
        public int CurrentHeaderPosition;
        public byte[] FileBytes;

        public ConvMessage ConvMessage;

        public FileInfo(ConvMessage convMessage, byte[] fileBytes)
        {
            ConvMessage = convMessage;
            FileBytes = fileBytes;

            if (FileBytes == null)
            {
                if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                    FileBytes = Encoding.UTF8.GetBytes(convMessage.MetaDataString);
                else
                    MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                                Convert.ToString(convMessage.MessageId), out FileBytes);
            }
        }
    }
}
