using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace windows_client.utils
{
    public class StickerCategory
    {
        public static readonly string STICKERS_DIR = "stickers";
        public static readonly string HIGH_RESOLUTION_DIR = "highres";
        public static readonly string LOW_RESOLUTION_DIR = "lowres";
        public static readonly string METADATA = "hasmorestickers";
        string _category;
        bool _hasMoreStickers = true;
        bool showDownloadMessage = true;
        public Dictionary<string, Sticker> dictStickers = new Dictionary<string, Sticker>();
        private bool isDownLoading;
        private static object readWriteLock = new object();

        public string Category
        {
            get
            {
                return _category;
            }
        }

        public bool IsDownLoading
        {
            get
            {
                return isDownLoading;
            }
            set
            {
                isDownLoading = value;
            }
        }

        public bool HasMoreStickers
        {
            get
            {
                return _hasMoreStickers;
            }
        }

        public bool ShowDownloadMessage
        {
            get
            {
                return showDownloadMessage;
            }
            set
            {
                showDownloadMessage = value;
            }
        }
        public StickerCategory(string category, bool hasMoreStickers)
        {
            this._category = category;
            this._hasMoreStickers = hasMoreStickers;
        }

        public StickerCategory(string category)
        {
            this._category = category;
        }

        
        public void CreateFromFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    Dictionary<string, Byte[]> dictImageBytes = new Dictionary<string, Byte[]>();
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category;
                        string[] files = store.GetFileNames(folder + "\\*");
                        if (files != null)
                            foreach (string stickerId in files)
                            {
                                string fileName = folder + "\\" + stickerId;
                                using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                                {
                                    using (var reader = new BinaryReader(file))
                                    {
                                        if (stickerId == METADATA)
                                        {
                                            _hasMoreStickers = reader.ReadBoolean();
                                            showDownloadMessage = reader.ReadBoolean();
                                        }
                                        else
                                        {
                                            int imageBytesCount = reader.ReadInt32();
                                            dictImageBytes.Add(stickerId, reader.ReadBytes(imageBytesCount));
                                        }
                                    }
                                }
                            }
                        Deployment.Current.Dispatcher.BeginInvoke(()=>
                            {
                                foreach(string stickerId in dictImageBytes.Keys)
                                {
                                    this.dictStickers.Add(stickerId,new Sticker(_category,stickerId,UI_Utils.Instance.createImageFromBytes(dictImageBytes[stickerId])));
                                }
                            });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
                }
            }
        }

        public void WriteHighResToFile(Dictionary<string, Byte[]> dictStcikers)
        {
            lock (readWriteLock)
            {
                if (dictStcikers != null && dictStcikers.Count > 0)
                {
                    try
                    {
                        string folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + _category;
                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                        {
                            if (!store.DirectoryExists(STICKERS_DIR))
                            {
                                store.CreateDirectory(STICKERS_DIR);
                            }
                            if (!store.DirectoryExists(STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR))
                            {
                                store.CreateDirectory(STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR);
                            }
                            if (!store.DirectoryExists(STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + _category))
                            {
                                store.CreateDirectory(STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + _category);
                            }
                            foreach (string stickerId in dictStcikers.Keys)
                            {
                                string fileName = folder + "\\" + stickerId;

                                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    using (BinaryWriter writer = new BinaryWriter(file))
                                    {
                                        Byte[] imageBytes = dictStcikers[stickerId];
                                        writer.Write(imageBytes.Length);
                                        writer.Write(imageBytes);
                                        writer.Flush();
                                        writer.Close();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StickerCategory::WriteToFile, Exception:" + ex.Message);
                    }
                }
            }
        }

        public void WriteLowResToFile(Dictionary<string, Byte[]> dictStcikers, bool hasMoreStickers)
        {
            lock (readWriteLock)
            {
                if (dictStcikers != null && dictStcikers.Count > 0)
                {
                    try
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category;

                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                        {
                            if (!store.DirectoryExists(STICKERS_DIR))
                            {
                                store.CreateDirectory(STICKERS_DIR);
                            }
                            if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR))
                            {
                                store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR);
                            }
                            if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category))
                            {
                                store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category);
                            }

                            foreach (string stickerId in dictStcikers.Keys)
                            {
                                string fileName = folder + "\\" + stickerId;

                                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    using (BinaryWriter writer = new BinaryWriter(file))
                                    {
                                        Byte[] imageBytes = dictStcikers[stickerId];
                                        writer.Write(imageBytes.Length);
                                        writer.Write(imageBytes);
                                        writer.Flush();
                                        writer.Close();
                                    }
                                }
                            }
                            string metadataFile = folder + "\\" + METADATA;
                            using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    _hasMoreStickers = hasMoreStickers;
                                    writer.Write(_hasMoreStickers);
                                    writer.Write(showDownloadMessage);
                                    writer.Flush();
                                    writer.Close();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StickerCategory::WriteToFile, Exception:" + ex.Message);
                    }
                }
            }
        }

        public void SetDownloadMessage(bool showDownloadMessage)
        {
            lock (readWriteLock)
            {
                try
                {
                    string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(STICKERS_DIR))
                        {
                            store.CreateDirectory(STICKERS_DIR);
                        }
                        if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR))
                        {
                            store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR);
                        }
                        if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category))
                        {
                            store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + _category);
                        }
                        string metadataFile = folder + "\\" + METADATA;
                        using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                this.showDownloadMessage = showDownloadMessage;
                                writer.Write(_hasMoreStickers);
                                writer.Write(showDownloadMessage);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::SetDownloadMessage, Exception:" + ex.Message);
                }
            }
        }

        public static BitmapImage GetStickerFromDb(string stickerId, string category)
        {
            if (string.IsNullOrEmpty(stickerId) || string.IsNullOrEmpty(category))
                return null;
            if (category == StickerHelper.CATEGORY_1)
            {
                return new BitmapImage(new Uri(string.Format("/View/images/stickers/{0}", stickerId), UriKind.Relative));
            }
            else
            {
                try
                {
                    lock (readWriteLock)
                    {
                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                        {
                            string fileName = StickerCategory.STICKERS_DIR + "\\" + StickerCategory.HIGH_RESOLUTION_DIR + "\\" + category + "\\" + stickerId;
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    int imageBytesCount = reader.ReadInt32();
                                    Byte[] imageBytes = reader.ReadBytes(imageBytesCount);
                                    return UI_Utils.Instance.createImageFromBytes(imageBytes);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
                }
            }
            return null;
        }


    }
}
