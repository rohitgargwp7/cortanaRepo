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
    public class UploadFileInfo
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
                return ((double)BytesTransfered / TotalBytes) * 100;
            }
        }

        public int TotalBytes
        {
            get
            {
                return FileBytes.Length;
            }
        }

        public int BlockSize = 1024;
        public int AttemptNumber = 1;

        public string SessionId;
        public int CurrentHeaderPosition;
        public byte[] FileBytes;
        public string ContentType;
        public string FileName;
        public JObject SuccessObj;

        public UploadFileState FileState;

        public UploadFileInfo()
        {
        }

        public UploadFileInfo(string key, byte[] fileBytes, string fileName, string contentType)
        {
            SessionId = key;
            FileBytes = fileBytes;
            ContentType = contentType;
            FileName = fileName;
            FileState = UploadFileState.NOT_STARTED;
            FileBytes = fileBytes;
        }

        public void Write(BinaryWriter writer)
        {
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
        }

        public void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
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

            FileState = (UploadFileState)reader.ReadInt32();

            if (App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && FileState == UploadFileState.STARTED)
                FileState = UploadFileState.PAUSED;

            count = reader.ReadInt32();
            FileBytes = count != 0 ? reader.ReadBytes(count) : FileBytes = null;
        }
    }

    public enum UploadFileState
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
