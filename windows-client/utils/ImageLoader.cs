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
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace windows_client.utils
{
    /// <summary>
    /// The Info associated with any Image is stored using an object of this class
    /// </summary>
    /// 
    /// <value>
    /// The value stored by this class are the BitmapImage imagesource, DefaultImageUri, HttpUri and filename
    /// </value>
    public class ImageInfo
    {
        public ImageInfo(BitmapImage imageSource, Uri uri, Uri defaultImgUrl, String fileName = null)
        {
            BitmapImage = imageSource;
            DefaultImgUri = defaultImgUrl;
            Uri = uri;
            FileName = String.IsNullOrEmpty(fileName) ? Utils.ConvertUrlToFileName(Uri.OriginalString) : fileName;
        }

        public BitmapImage BitmapImage;
        public Uri DefaultImgUri;
        public Uri Uri;

        public String FileName
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// The class handles downloading of images from the net and storing them to localCache or Isolatedstorage.
    /// </summary>
    public static class ImageLoader
    {
        /// <summary>
        /// Constructor linking the webclient's OpenReadCompleted event. 
        /// On completion invokes the _client_OpenReadCompleted function
        /// </summary>
        static ImageLoader()
        {
        }

        #region "Isolated Storage"
        
        /// <summary>
        /// Read from the Isolated Storage
        /// </summary>
        /// 
        /// <param name="imgInfo">
        /// Info about the image which needs to be read from Isolated Storage
        /// </param>
        /// 
        /// <returns>
        /// Image in Bytes if found else null
        /// </returns>
        private static Byte[] ReadFromIsolatedStorage(ImageInfo imgInfo)
        {
            string fileName = "ProTips//" + imgInfo.FileName;
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myIsolatedStorage.FileExists(fileName))
                    return null;

                MemoryStream retStream = new MemoryStream();

                try
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(retStream);
                        fileStream.Close();
                    }
                }
                catch
                {
                    return null;
                }
               
                return retStream.Length == 0 ? null : retStream.GetBuffer();
            }
        }

        /// <summary>
        /// Writes ImageBytes to Isolated Storage
        /// </summary>
        /// 
        /// <param name="imgInfo">
        /// Information of image which needs to be stored in the Isolated Storage
        /// </param>
        /// 
        /// <param name="imageData">
        /// Image in bytes which needs to be stored in the Isolated Storage
        /// </param>
        private static void SaveFileInIsolatedStorage(String fileName, Byte[] imageData)
        {
            System.Windows.Resources.StreamResourceInfo streamResourceInfo = Application.GetResourceStream(new Uri(fileName, UriKind.Relative));

            // Create virtual store and file stream. Check for duplicate tempJPEG files.
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                fileName = "ProTips//" + fileName;

                if (myIsolatedStorage.FileExists(fileName))
                    myIsolatedStorage.DeleteFile(fileName);

                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(fileName, FileMode.Create, myIsolatedStorage))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        Stream resourceStream = new MemoryStream(imageData);
                        long length = resourceStream.Length;
                        byte[] buffer = new byte[32];
                        int readCount = 0;

                        using (resourceStream)
                        {   
                            resourceStream.Seek(0, SeekOrigin.Begin);

                            // Read file in chunks in order to reduce memory consumption and increase performance
                            while (readCount < length)
                            {
                                int actual = resourceStream.Read(buffer, 0, buffer.Length);
                                readCount += actual;
                                writer.Write(buffer, 0, actual);
                            }
                        }
                    }

                    fileStream.Close();
                }
            }
        }

        #endregion

        private static void LoadReq()
        {
            if (Sources.Count > 0)
            {
                ImageInfo imgInfo = Sources[Sources.Count - 1];

                Byte[] bytes = ReadFromIsolatedStorage(imgInfo);

                if (bytes == null)
                    Download(imgInfo);
                else
                    Deployment.Current.Dispatcher.BeginInvoke(new Action<ImageInfo, Byte[]>(SetImageSource), imgInfo, bytes);
            }
        }

        static System.ComponentModel.BackgroundWorker _loadWorker;

        public static void Load(BitmapImage imageSource, Uri uri, Uri defaultImgUrl = null, String fileName = null)
        {
            imageSource.CreateOptions = BitmapCreateOptions.DelayCreation;
            ImageInfo imgInfo = new ImageInfo(imageSource, uri, defaultImgUrl, fileName);
            Sources.Add(imgInfo);

            if (_loadWorker == null)
            {
                _loadWorker = new System.ComponentModel.BackgroundWorker();
                _loadWorker.DoWork += delegate
                {
                    LoadReq();
                };

                _loadWorker.RunWorkerCompleted += delegate
                {
                    if (Sources.Count > 0)
                    {
                        if (!_loadWorker.IsBusy)
                            _loadWorker.RunWorkerAsync();
                    }
                };
            }

            if (!_loadWorker.IsBusy)
                _loadWorker.RunWorkerAsync();
        }

        private static void Download(ImageInfo imgInfo)
        {
            AccountUtils.createGetRequest(imgInfo.Uri.OriginalString, getProTipPic_Callback, true, Utils.ConvertUrlToFileName(imgInfo.FileName));
        }

        public static void getProTipPic_Callback(byte[] fullBytes, object fName)
        {   
            try
            {
                string fileName = fName as string;
                ImageInfo imgInfo = Sources.Where(i => i.FileName == fileName).First() as ImageInfo;

                if (imgInfo != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(new Action<ImageInfo, Byte[]>(SetImageSource), imgInfo as ImageInfo, fullBytes);
                    SaveFileInIsolatedStorage(fName as String, fullBytes);
                    Sources.Remove(imgInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            if (!_loadWorker.IsBusy)
                _loadWorker.RunWorkerAsync();
        }

        private static void SetImageSource(ImageInfo imgInfo, Byte[] imgData)
        {
            try
            {
                Stream data = new MemoryStream(imgData);
                imgInfo.BitmapImage.SetSource(data);
            }
            catch 
            {
                System.Diagnostics.Debug.WriteLine("Image Render Failed, " + imgInfo.Uri);
                imgInfo.BitmapImage.UriSource = imgInfo.DefaultImgUri;
            }
        }

       
        /// <summary>
        /// Queue of Images which have been downloaded but need to be linked to their respective imageSource
        /// </summary>
        static List<ImageInfo> Sources = new List<ImageInfo>();
    }
}