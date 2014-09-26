﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using System.Windows;

namespace windows_client.utils
{
    class ProTipHelper
    {
        private const string PROTIPS_DIRECTORY = "ProTips";
        private const string CURRENT_PROTIP_IMAGE = "CurrentProtipImage";

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        private static volatile ProTipHelper instance = null;
        private static ProTip _currentProTip = null;

        public event EventHandler<EventArgs> ShowProTip;

        public static ProTip CurrentProTip
        {
            get
            {
                return _currentProTip;
            }
            set
            {
                _currentProTip = value;
            }
        }

        public static ProTipHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new ProTipHelper();
                            string id = String.Empty;
                            HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettingsKeys.PRO_TIP, out id);

                            if (!String.IsNullOrEmpty(id))
                                ReadProTipFromFile(id);
                        }
                    }
                }
                return instance;
            }
        }

        public void AddProTip(string id, string header, string body, string imageUrl, string base64Image)
        {
            RemoveCurrentProTip();

            CurrentProTip = new ProTip(id, header, body, imageUrl, base64Image);

            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettingsKeys.PRO_TIP, id);

            WriteProTipToFile();

            if (ShowProTip != null)
                ShowProTip(null, null);
        }

        public void saveProTipImage(byte[] imageBytes)
        {
            if (imageBytes == null)
                return;

            lock (readWriteLock)
            {
                try
                {
                    string FileName = PROTIPS_DIRECTORY + "\\" + CURRENT_PROTIP_IMAGE;

                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (store.FileExists(FileName))
                            store.DeleteFile(FileName);

                        using (var file = store.OpenFile(FileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(imageBytes.Length);
                                writer.Write(imageBytes);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTipHelper :: saveProTipImage : saveProTipImage, Exception : " + ex.StackTrace);
                }
            }
        }

        public byte[] getProTipImage()
        {
            byte[] imageBytes = null;
            lock (readWriteLock)
            {
                try
                {
                    string FileName = PROTIPS_DIRECTORY + "\\" + CURRENT_PROTIP_IMAGE;

                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.FileExists(FileName))
                            return null;

                        using (var file = store.OpenFile(FileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                var count = reader.ReadInt32();
                                imageBytes = reader.ReadBytes(count);
                                reader.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTipHelper :: getProTipImage : getProTipImage, Exception : " + ex.StackTrace);
                }
            }

            return imageBytes;
        }

        public void WriteProTipToFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + CurrentProTip._id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(PROTIPS_DIRECTORY))
                            store.CreateDirectory(PROTIPS_DIRECTORY);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                CurrentProTip.Write(writer);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Write ProTip To File, Exception : " + ex.StackTrace);
                }
            }
        }

        static void ReadProTipFromFile(String id)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(PROTIPS_DIRECTORY))
                            return;

                        if (!store.FileExists(fileName))
                            return;

                        CurrentProTip = new ProTip();

                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                CurrentProTip.Read(reader);
                                reader.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Read ProTip From File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void ClearProTips()
        {
            if (CurrentProTip != null)
                RemoveCurrentProTip();

            ClearOldProTips();

            HikeInstantiation.AppSettings.Remove(HikeConstants.AppSettingsKeys.PRO_TIP);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.PRO_TIP_COUNT);
        }

        public void ClearOldProTips()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (!store.DirectoryExists(PROTIPS_DIRECTORY))
                        return;

                    var fileNames = store.GetFileNames(PROTIPS_DIRECTORY + "\\*");

                    if (CurrentProTip != null)
                    {
                        var currentFile = CurrentProTip.ImageUrl != null ? PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(CurrentProTip.ImageUrl) : String.Empty;

                        var currentTipFile = PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(CurrentProTip._id);

                        foreach (var fileName in fileNames)
                        {
                            if (fileName != currentFile && fileName != currentTipFile && store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                    }
                    else
                    {
                        foreach (var fileName in fileNames)
                        {
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete ProTip Data on upgrade to 2.2.2.1, Exception : " + ex.StackTrace);
                }
            }
        }

        public void RemoveCurrentProTip()
        {
            if (CurrentProTip == null)
                return;

            if (!String.IsNullOrEmpty(CurrentProTip.ImageUrl))
            {
                lock (readWriteLock)
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            string fileName = PROTIPS_DIRECTORY + "\\" + CurrentProTip._id;

                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);

                            if (CurrentProTip.ImageUrl != null)
                            {
                                fileName = PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(CurrentProTip.ImageUrl);

                                if (store.FileExists(fileName))
                                    store.DeleteFile(fileName);
                            }

                            fileName = PROTIPS_DIRECTORY + "\\" + CURRENT_PROTIP_IMAGE;

                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete current ProTip File, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }

            CurrentProTip = null;
        }
    }

    public class ProTip
    {
        public string _id;
        public string _header;
        public string _body;
        public string ImageUrl;
        public string Base64Image;

        ImageSource _tipImage;
        public ImageSource TipImage
        {
            get
            {
                if (_tipImage == null)
                {
                    if (Base64Image != null)
                        _tipImage = UI_Utils.Instance.createImageFromBytes(System.Convert.FromBase64String(Base64Image));
                    else
                        _tipImage = ProcesImageSource();

                    return _tipImage;
                }
                else
                    return _tipImage;
            }
        }

        private ImageSource ProcesImageSource()
        {
            ImageSource source = null;

            source = new BitmapImage();

            if (!String.IsNullOrEmpty(ImageUrl))
                ImageLoader.Load(source as BitmapImage, new Uri(ImageUrl), null, Utils.ConvertUrlToFileName(ImageUrl));

            return source;
        }

        public ProTip() { }

        public ProTip(string id, string header, string body, string imageUrl, string base64Image)
        {
            _id = id;
            _header = header;
            _body = body;
            ImageUrl = imageUrl;
            Base64Image = base64Image;
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(_id);

                if (_header == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_header);

                if (_body == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_body);

                if (ImageUrl == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(ImageUrl);

                if (Base64Image == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(Base64Image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ProTip :: Write : Unable To write, Exception : " + ex.StackTrace);
            }

        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                count = reader.ReadInt32();
                _header = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (_header == "*@N@*")
                    _header = null;

                count = reader.ReadInt32();
                _body = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (_body == "*@N@*")
                    _body = null;

                count = reader.ReadInt32();
                ImageUrl = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                if (ImageUrl == "*@N@*")
                    ImageUrl = null;

                try
                {
                    count = reader.ReadInt32();
                    Base64Image = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (Base64Image == "*@N@*")
                        Base64Image = null;
                }
                catch
                {
                    Base64Image = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ProTip :: Read : Read, Exception : " + ex.StackTrace);
            }
        }
    }
}
