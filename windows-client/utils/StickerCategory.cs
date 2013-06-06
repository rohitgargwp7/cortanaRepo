using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using windows_client.ViewModel;

namespace windows_client.utils
{
    public class StickerCategory
    {
        public const string STICKERS_DIR = "stickers";
        public const string HIGH_RESOLUTION_DIR = "highres";
        public const string LOW_RESOLUTION_DIR = "lowres";
        public const string METADATA = "metadata";

        private string _category;
        private bool _hasMoreStickers = true;
        private bool _showDownloadMessage = true;
        private ObservableCollection<Sticker> _listStickers;
        private bool _isDownLoading;
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
                return _isDownLoading;
            }
            set
            {
                _isDownLoading = value;
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
                return _showDownloadMessage;
            }
            set
            {
                _showDownloadMessage = value;
            }
        }

        public ObservableCollection<Sticker> ListStickers
        {
            get
            {
                return _listStickers;
            }
        }
        public StickerCategory(string category, bool hasMoreStickers)
            : this(category)
        {
            this._hasMoreStickers = hasMoreStickers;
        }

        public StickerCategory(string category)
        {
            this._category = category;
            _listStickers = new ObservableCollection<Sticker>();
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
                                            _showDownloadMessage = reader.ReadBoolean();
                                        }
                                        else
                                        {
                                            int imageBytesCount = reader.ReadInt32();
                                            dictImageBytes.Add(stickerId, reader.ReadBytes(imageBytesCount));
                                        }
                                    }
                                }
                            }
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                foreach (string stickerId in dictImageBytes.Keys)
                                {
                                    this._listStickers.Add(new Sticker(_category, stickerId, UI_Utils.Instance.createImageFromBytes(dictImageBytes[stickerId])));
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

        public void WriteHighResToFile(List<KeyValuePair<string, Byte[]>> listStickersImageBytes)
        {
            lock (readWriteLock)
            {
                if (listStickersImageBytes != null && listStickersImageBytes.Count > 0)
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
                            foreach (KeyValuePair<string, Byte[]> keyValuePair in listStickersImageBytes)
                            {
                                string fileName = folder + "\\" + keyValuePair.Key;
                                try
                                {
                                    Byte[] imageBytes = keyValuePair.Value;
                                    if (imageBytes == null || imageBytes.Length == 0)
                                        continue;
                                    using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                    {
                                        using (BinaryWriter writer = new BinaryWriter(file))
                                        {
                                            writer.Write(imageBytes.Length);
                                            writer.Write(imageBytes);
                                            writer.Flush();
                                            writer.Close();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("Writing HIgh res Sticker:{0} failed,Exception:{1}", fileName, ex.Message);
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

        public void WriteLowResToFile(List<KeyValuePair<string, Byte[]>> listStickersImageBytes, bool hasMoreStickers)
        {
            lock (readWriteLock)
            {
                if ((listStickersImageBytes != null && listStickersImageBytes.Count > 0) || hasMoreStickers)
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

                            foreach (KeyValuePair<string, byte[]> keyValuePair in listStickersImageBytes)
                            {
                                string fileName = folder + "\\" + keyValuePair.Key;

                                try
                                {
                                    Byte[] imageBytes = keyValuePair.Value;
                                    if (imageBytes == null || imageBytes.Length == 0)
                                        continue;
                                    using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                    {
                                        using (BinaryWriter writer = new BinaryWriter(file))
                                        {
                                            writer.Write(imageBytes == null ? 0 : imageBytes.Length);
                                            writer.Write(imageBytes);
                                            writer.Flush();
                                            writer.Close();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("Writing Sticker:{0} failed,Exception:{1}", fileName, ex.Message);
                                }
                            }
                            string metadataFile = folder + "\\" + METADATA;
                            using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    _hasMoreStickers = hasMoreStickers;
                                    writer.Write(_hasMoreStickers);
                                    writer.Write(_showDownloadMessage);
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
                                this._showDownloadMessage = showDownloadMessage;
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

        public static BitmapImage GetHighResolutionSticker(string stickerId, string category)
        {
            if (string.IsNullOrEmpty(stickerId) || string.IsNullOrEmpty(category))
                return null;
            if (category == StickerHelper.CATEGORY_1)
            {
                string url;
                if (Utils.CurrentResolution == Utils.Resolutions.WXGA)
                    url = StickerHelper._stickerWXGApath;
                else if (Utils.CurrentResolution == Utils.Resolutions.WVGA)
                    url = StickerHelper._stickerWVGAPath;
                else
                    url = StickerHelper._sticker720path;
                return new BitmapImage(new Uri(string.Format(url, stickerId), UriKind.Relative));
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

        public static void CreateCategory(string category)
        {
            lock (readWriteLock)
            {
                string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

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
                    if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category))
                    {
                        store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category);
                    }
                }

            }
        }

        public static List<StickerCategory> ReadAllStickerCategories()
        {
            List<StickerCategory> listStickerCategory = new List<StickerCategory>();
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR;
                        string[] folders = store.GetDirectoryNames(folder + "\\*");
                        if (folders != null)
                            foreach (string category in folders)
                            {
                                List<KeyValuePair<string, Byte[]>> listImageBytes = new List<KeyValuePair<string, Byte[]>>();
                                StickerCategory stickerCategory = new StickerCategory(category);
                                string[] files = store.GetFileNames(folder + "\\" + category + "\\*");
                                if (files != null)

                                    foreach (string stickerId in files)
                                    {
                                        string fileName = folder + "\\" + category + "\\" + stickerId;
                                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                                        {
                                            using (var reader = new BinaryReader(file))
                                            {
                                                try
                                                {
                                                    if (stickerId == METADATA)
                                                    {
                                                        stickerCategory._hasMoreStickers = reader.ReadBoolean();
                                                        stickerCategory._showDownloadMessage = reader.ReadBoolean();
                                                    }
                                                    else
                                                    {
                                                        int imageBytesCount = reader.ReadInt32();
                                                        listImageBytes.Add(new KeyValuePair<string, Byte[]>(stickerId, reader.ReadBytes(imageBytesCount)));
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug.WriteLine("Exception in reading sticker file,message:" + ex.Message);
                                                }
                                            }
                                        }
                                    }
                                Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    foreach (KeyValuePair<string, Byte[]> keyValuePair in listImageBytes)
                                    {

                                        stickerCategory._listStickers.Add(new Sticker(category, keyValuePair.Key, UI_Utils.Instance.createImageFromBytes(keyValuePair.Value)));
                                    }
                                });
                                listStickerCategory.Add(stickerCategory);
                            }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
                }
                return listStickerCategory;
            }
        }

        public static void DeleteCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return;
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string[] files = store.GetFileNames(folder + "\\*");
                            if (files != null)
                                foreach (string stickerId in files)
                                {
                                    string fileName = folder + "\\" + stickerId;
                                    if (store.FileExists(fileName))
                                        store.DeleteFile(fileName);
                                }
                            store.DeleteDirectory(folder);
                        }

                        folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string[] files = store.GetFileNames(folder + "\\*");
                            if (files != null)
                                foreach (string stickerId in files)
                                {
                                    string fileName = folder + "\\" + stickerId;
                                    if (store.FileExists(fileName))
                                        store.DeleteFile(fileName);
                                }
                            store.DeleteDirectory(folder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }

        public static void DeleteSticker(string category, string stickerId)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(stickerId))
                return;
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string fileName = folder + "\\" + stickerId;
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }

                        folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string fileName = folder + "\\" + stickerId;
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }

        public static void DeleteSticker(string category, List<string> listStickerIds)
        {
            if (string.IsNullOrEmpty(category) || listStickerIds == null || listStickerIds.Count == 0)
                return;
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            foreach (string stickerId in listStickerIds)
                            {
                                string fileName = folder + "\\" + stickerId;
                                if (store.FileExists(fileName))
                                    store.DeleteFile(fileName);
                            }
                        }

                        folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            foreach (string stickerId in listStickerIds)
                            {
                                string fileName = folder + "\\" + stickerId;
                                if (store.FileExists(fileName))
                                    store.DeleteFile(fileName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }

        public static void UpdateHasMoreMessages(string category, bool hasMoreStickers)
        {
            if (string.IsNullOrEmpty(category))
                return;

            if (HikeViewModel.stickerHelper != null && HikeViewModel.stickerHelper.GetStickersByCategory(category) != null)
            {
                HikeViewModel.stickerHelper.GetStickersByCategory(category)._hasMoreStickers = hasMoreStickers;
            }
            lock (readWriteLock)
            {
                try
                {
                    string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;
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
                        if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category))
                        {
                            store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category);
                        }
                        string metadataFile = folder + "\\" + METADATA;
                        using (var file = store.OpenFile(metadataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            bool showDownloadMessage = true;
                            if (file.Length > 0)
                            {
                                using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                {
                                    try
                                    {
                                        reader.ReadBoolean();
                                        showDownloadMessage = reader.ReadBoolean();
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Write(hasMoreStickers);
                                writer.Write(showDownloadMessage);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::UpdateHasMoreMessages, Exception:" + ex.Message);
                }
            }
        }
    }
}
