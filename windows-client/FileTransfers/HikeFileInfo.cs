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
    public class HikeFileInfo
    {
        public int BytesTransfered
        {
            get
            {
                return CurrentHeaderPosition == 0 ? 0 : CurrentHeaderPosition - 1;
            }
        }

        public double PercentageTransfer
        {
            get 
            {
                return TotalBytes == 0 ? 0 : ((double)BytesTransfered / TotalBytes) * 100;
            }
        }

        public int TotalBytes;
        public int BlockSize = 1024;
        public int AttemptNumber = 1;
        public string SessionId;
        public int CurrentHeaderPosition;
        public byte[] FileBytes;
        public string ContentType;
        public string FileName;
        public string Msisdn;
        public JObject SuccessObj;
        public HikeFileState FileState;
        public bool IsDownload;

        public HikeFileInfo()
        {
        }

        public HikeFileInfo(string msisdn, string key, byte[] fileBytes, string fileName, string contentType, bool isDownload)
        {
            Msisdn = msisdn;
            SessionId = key;
            FileBytes = fileBytes;
            ContentType = contentType;
            FileName = fileName;
            FileState = HikeFileState.NOT_STARTED;
            FileBytes = fileBytes;
            IsDownload = isDownload;

            if (!isDownload)
                TotalBytes = fileBytes.Length;
        }

        public void Write(BinaryWriter writer)
        {
            if (Msisdn == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Msisdn);
            
            if (SessionId == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(SessionId);

            writer.Write(CurrentHeaderPosition);

            if (SuccessObj == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(SuccessObj.ToString(Newtonsoft.Json.Formatting.None));

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

            writer.Write(TotalBytes);
            writer.Write(IsDownload);
        }

        public void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Msisdn == "*@N@*")
                Msisdn = null;
            
            count = reader.ReadInt32();
            SessionId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (SessionId == "*@N@*")
                SessionId = null;

            CurrentHeaderPosition = reader.ReadInt32();

            count = reader.ReadInt32();
            var str = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (str == "*@N@*")
                SuccessObj = null;
            else
                SuccessObj = JObject.Parse(str);

            count = reader.ReadInt32();
            FileName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (FileName == "*@N@*")
                FileName = null;

            count = reader.ReadInt32();
            ContentType = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (ContentType == "*@N@*")
                ContentType = null;

            FileState = (HikeFileState)reader.ReadInt32();

            if (App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && FileState == HikeFileState.STARTED)
                FileState = HikeFileState.PAUSED;

            count = reader.ReadInt32();
            FileBytes = count != 0 ? reader.ReadBytes(count) : FileBytes = null;

            TotalBytes = reader.ReadInt32();
            IsDownload = reader.ReadBoolean();
        }
    }

    public enum HikeFileState
    {
        NOT_STARTED,
        FAILED,  
        STARTED,
        COMPLETED,
        CANCELED,
        PAUSED,
        MANUAL_PAUSED
    }
}
