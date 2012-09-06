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
using System.ComponentModel;

namespace windows_client.Model
{
    public class Attachment
    {
        private string _fileKey;
        private string _fileName;
        private string _contentType;
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

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                if (_contentType != value)
                {
                    _contentType = value;
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

        //newly received messages
        public Attachment(string fileName, string fileKey, byte[] thumbnailBytes, string contentType)
        {
            this.FileName = fileName;
            this.FileKey = fileKey;
            this.Thumbnail = thumbnailBytes;
            this.ContentType = contentType;
        }

        public Attachment(string fileName, byte[] thumbnailBytes)
        {
            this.FileName = fileName;
            this.Thumbnail = thumbnailBytes;
        }


        public static void storeFileInIsolatedStorage(string filePath, byte[] imagebytes)
        {
            string fileDirectory = filePath.Substring(0, filePath.LastIndexOf("/"));
            if (imagebytes != null)
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!myIsolatedStorage.DirectoryExists(fileDirectory))
                    {
                        myIsolatedStorage.CreateDirectory(fileDirectory);
                    }

                    if (myIsolatedStorage.FileExists(filePath))
                    {
                        myIsolatedStorage.DeleteFile(filePath);
                    }

                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath, FileMode.Create, myIsolatedStorage))
                    {
                        using (BinaryWriter writer = new BinaryWriter(fileStream))
                        {
                            writer.Write(imagebytes, 0, imagebytes.Length);
                        }
                    }
                }
            }
        }

        public static string[] getAttachmentFiles(string msisdn)
        { 
            string filePath = HikeConstants.FILE_TRANSFER_LOCATION + "/" + "Attachments" + "/" + msisdn + "/*";
            string[] fileNames = null;
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    fileNames = myIsolatedStorage.GetFileNames(filePath);
                }
            }
            return fileNames;        
        }


        public static void readFileFromIsolatedStorage(string filePath, out byte[] imageBytes)
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                    {
                        imageBytes = new byte[fileStream.Length];
                        // Read the entire file and then close it
                        fileStream.Read(imageBytes, 0, imageBytes.Length);
                        fileStream.Close();
                    }
                }
                else
                {
                    imageBytes = null;
                }
            }
        }

        //while reading messages from db
        //public Attachment(long msgId)
        //{
        //    readFileFromIsolatedStorage(HikeConstants.FILE_TRANSFER_LOCATION + "/" + Convert.ToString(msgId), out _thumbnail);
        //}

    }
}
