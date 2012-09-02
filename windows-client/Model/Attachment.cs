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
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Resources;
using System.IO.IsolatedStorage;

namespace windows_client.Model
{
    public class Attachment
    {
        private string _fileKey;
        private string _fileName;
        private byte[] _thumbnail;

        public string FileKey
        {
            get
            {
                return _fileKey;
            }
            set
            {
                if (_fileKey != value)
                {
                    _fileKey = value;
                }
            }
        }

        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                }
            }
        }

        public byte[] Thumbnail
        {
            get
            {
                return _thumbnail;
            }
            set
            {
                if (_thumbnail != value)
                {
                    _thumbnail = value;
                }
            }
        }

        public Attachment(string fileName, string fileKey, byte[] thumbnailBytes)
        {
            this.FileName = fileName;
            this.FileKey = fileKey;
            this.Thumbnail = thumbnailBytes;


            //run a bg to download image and store to isolated storage
        }

        public void storeThumbnailInIsolatedStorage(long msgId)
        {
            string filePath = HikeConstants.FILE_TRANSFER + "/" + Convert.ToString(msgId);
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    myIsolatedStorage.DeleteFile(filePath);
                }

                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath, FileMode.Create, myIsolatedStorage))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        writer.Write(_thumbnail, 0, _thumbnail.Length);
                    }
                }
            }
        }

        public Attachment(long msgId)
        {
            string filePath = HikeConstants.FILE_TRANSFER + "/" + Convert.ToString(msgId);

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                {
                    _thumbnail = new byte[fileStream.Length];
                    // Read the entire file and then close it
                    fileStream.Read(_thumbnail, 0, _thumbnail.Length);
                    fileStream.Close();
                }
            }
        }

    }
}
