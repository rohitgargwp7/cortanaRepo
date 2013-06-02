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
            _client.OpenReadCompleted += new OpenReadCompletedEventHandler(_client_OpenReadCompleted);
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
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myIsolatedStorage.FileExists(imgInfo.FileName))
                    return null;

                MemoryStream retStream = new MemoryStream();

                try
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(imgInfo.FileName, FileMode.Open, FileAccess.Read))
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
        /// Clears older files and images stored by the app in isolated storage to free used memory
        /// </summary>
        public static void ClearOlderUnusedFilesFromIsolatedStorage()
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                String[] fileNames = myIsolatedStorage.GetFileNames();

                foreach (String fileName in fileNames)
                {
                    DateTimeOffset dtOffset = myIsolatedStorage.GetLastAccessTime(fileName);

                    if ((DateTime.Now - dtOffset.DateTime).TotalDays > 30)
                    {
                        if (myIsolatedStorage.FileExists(fileName))
                        {
                            myIsolatedStorage.DeleteFile(fileName);
                        }
                    }
                }
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
        private static void SaveFileInIsolatedStorage(ImageInfo imgInfo, Byte[] imageData)
        {   
            // Create a filename for JPEG file in isolated storage.
            String tempJPEG = imgInfo.FileName;
            System.Windows.Resources.StreamResourceInfo streamResourceInfo = Application.GetResourceStream(new Uri(tempJPEG, UriKind.Relative));

            // Create virtual store and file stream. Check for duplicate tempJPEG files.
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {

                Double availSpaceMb = (myIsolatedStorage.AvailableFreeSpace / 1024) / 1024;

                if (availSpaceMb < 1)
                    ClearOlderUnusedFilesFromIsolatedStorage();

                if (myIsolatedStorage.FileExists(tempJPEG))
                {   
                    myIsolatedStorage.DeleteFile(tempJPEG);
                }

                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(tempJPEG, FileMode.Create, myIsolatedStorage))
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

        /// <summary>
        /// The function takes in an ImageInfo and returns the imageStream in Byte if it exists in the local cache
        /// </summary>
        /// 
        /// <param name="imageInfo">
        /// Info of image Required
        /// </param>
        /// 
        /// <returns>
        /// Image in byte stored in localCache if present else null
        /// </returns>
        private static Byte[] ReadFromLocalImageCache(ImageInfo imageInfo)
        {
            if (LocalImageCache.Count> 0 && LocalImageCache.ContainsKey(imageInfo.FileName))
            {
                return LocalImageCache[imageInfo.FileName];
            }
            else
                return null;
        }

        /// <summary>
        /// Saves Image to LocalImageCache which is a dictionary
        /// </summary>
        /// 
        /// <param name="imgInfo">
        /// Info of image to be saved
        /// </param>
        /// <param name="imageData">
        /// Image in Bytes
        /// </param>
        private static void SaveToLocalImageCache(ImageInfo imgInfo, Byte[] imageData)
        {
            if (LocalImageCache.ContainsKey(imgInfo.FileName))
                LocalImageCache[imgInfo.FileName] = imageData;
            else
                LocalImageCache.Add(imgInfo.FileName, imageData);
        }
        
        /// <summary>
        /// Loading Image
        /// 1) from localcache
        /// 2) if not present then from isolatedstorage
        /// 3) if not present then download using webclient and saving to localCache
        /// </summary>
        /// 
        /// <param name="forceEntry">
        /// forcing the function to download the image even if the webclient is busy
        /// </param>
        private static void LoadReq()
        {   
            if (Sources.Count > 0)
            {   
                ImageInfo imgInfo = Sources.Dequeue();

                Byte[] bytes = ReadFromLocalImageCache(imgInfo);

                if (bytes == null)
                {   
                    bytes = ReadFromIsolatedStorage(imgInfo);

                    if (bytes == null)
                    {
                        Boolean isDownloadSuccessfullyPlaced = Download(imgInfo);

                        if (!isDownloadSuccessfullyPlaced)
                        {
                            // So need to wait for downloader. So add it back to the end of the queue
                            Sources.Enqueue(imgInfo);
                        }
                    }
                    else
                    {   
                        SaveToLocalImageCache(imgInfo, bytes);
                        //System.Threading.Thread.Sleep(400); lazy loading
                        Deployment.Current.Dispatcher.BeginInvoke(new Action<ImageInfo, Byte[]>(SetImageSource), imgInfo, bytes);
                    }
                }
                else
                {   
                    //System.Threading.Thread.Sleep(400); lazy loading
                    Deployment.Current.Dispatcher.BeginInvoke(new Action<ImageInfo, Byte[]>(SetImageSource), imgInfo, bytes);
                }
            }
        }

        static System.ComponentModel.BackgroundWorker _loadWorker;

        /// <summary>
        /// Called from ResultItemViewModel while assigning images to layout
        /// </summary>
        /// 
        /// <param name="imageSource">
        /// ImageSource to which the downloaded image needs to be attached with
        /// </param>
        /// 
        /// <param name="uri">
        /// URI of the image to be downloaded
        /// </param>
        /// 
        /// <param name="DefaultImgUrl">
        /// If Image fails due to network problems or if its a gif image, display a default Image
        /// </param>
        public static void Load(BitmapImage imageSource, Uri uri, Uri defaultImgUrl = null, String fileName = null)
        {
            imageSource.CreateOptions = BitmapCreateOptions.DelayCreation;
            ImageInfo imgInfo = new ImageInfo(imageSource, uri, defaultImgUrl, fileName);
            Sources.Enqueue(imgInfo);

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

        /// <summary>
        /// Downloads the Image using the uri provided via a webclient in the background
        /// <returns>
        /// Whether Download request is successfully placed
        /// </returns>
        /// </summary>
        private static Boolean Download(ImageInfo imgInfo)
        {   
            if (_client.IsBusy)
                return false;
            else
            {
                System.Threading.Thread.Sleep(100);
                _client.OpenReadAsync(imgInfo.Uri, imgInfo);
                return true;
            }
        }

        /// <summary>
        /// Invoked after download of image. 
        /// 
        /// Saves it to local cache and isolated storage both. 
        /// And links the image to the bitmap imagesource.
        /// </summary>
        /// 
        /// <param name="sender"></param>
        /// <param name="e">
        /// Contains the image downloaded as Result
        /// </param>
        static void _client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {   
            try
            {   
                var resInfo = new System.Windows.Resources.StreamResourceInfo(e.Result, null);
                var reader = new StreamReader(resInfo.Stream);
                byte[] myByte;

                using (BinaryReader bReader = new BinaryReader(reader.BaseStream))
                {
                    myByte = bReader.ReadBytes((int)reader.BaseStream.Length);
                }

                System.Diagnostics.Debug.WriteLine("Image Was Under Download " + (e.UserState as ImageInfo).Uri);
                Deployment.Current.Dispatcher.BeginInvoke(new Action<ImageInfo, Byte[]>(SetImageSource), e.UserState as ImageInfo, myByte);

                SaveToLocalImageCache(e.UserState as ImageInfo, myByte);
                SaveFileInIsolatedStorage(e.UserState as ImageInfo, myByte);
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            if (!_loadWorker.IsBusy)
                _loadWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Maps the image downloaded as bytes to imageSource present in the ImageInfo
        /// </summary>
        /// 
        /// <param name="imgInfo">
        /// Stores the imageSource to which the image needs to be mapped
        /// </param>
        /// <param name="imgData">
        /// The Image in Bytes. 
        /// Needs to be converted into Stream/MemoryStream inorder to link it to BitmapImageSource
        /// </param>
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
        static Queue<ImageInfo> Sources = new Queue<ImageInfo>();

        /// <summary>
        /// Dictionary to maintain images in LocalCache
        /// </summary>
        public static Dictionary<String, Byte[]> LocalImageCache = new Dictionary<String, Byte[]>();
        
        /// <summary>
        /// Webclient to download Images from http uri
        /// </summary>
        private static WebClient _client = new WebClient();
    }
}