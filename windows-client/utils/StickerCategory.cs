﻿using Newtonsoft.Json.Linq;
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
        private bool _hasNewStickers = false;
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

        /// <summary>
        /// currenty request has been sent to server for download
        /// </summary>
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

        /// <summary>
        /// shows server has more stickers for download
        /// </summary>
        public bool HasMoreStickers
        {
            get
            {
                return _hasMoreStickers;
            }
            set
            {
                _hasMoreStickers = value;
            }
        }

        /// <summary>
        /// to show stickers download overlay
        /// </summary>
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

        /// <summary>
        /// shows category has newly downloaded stickers
        /// </summary>
        public bool HasNewStickers
        {
            get
            {
                return _hasNewStickers;
            }
            set
            {
                _hasNewStickers = value;
            }
        }

        public ObservableCollection<Sticker> ListStickers
        {
            get
            {
                return _listStickers;
            }
            set
            {
                _listStickers = value;
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
                if (listStickersImageBytes != null)
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
                                    writer.Write(_hasNewStickers);
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
                                writer.Write(_hasNewStickers);
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

        public static BitmapImage GetHighResolutionSticker(Sticker sticker)
        {
            if (sticker == null || string.IsNullOrEmpty(sticker.Id) || string.IsNullOrEmpty(sticker.Category))
                return null;

            if ((sticker.Category == StickerHelper.CATEGORY_DOGGY && StickerHelper.arrayDefaultDoggyStickers.Contains(sticker.Id))
                || (sticker.Category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.arrayDefaultHumanoidStickers.Contains(sticker.Id)))
            {
                string url;
                if (Utils.CurrentResolution == Utils.Resolutions.WXGA)
                    url = StickerHelper._stickerWXGApath;
                else if (Utils.CurrentResolution == Utils.Resolutions.WVGA)
                    url = StickerHelper._stickerWVGAPath;
                else
                    url = StickerHelper._sticker720path;
                return new BitmapImage(new Uri(string.Format(url, sticker.Category, sticker.Id), UriKind.Relative));
            }

            try
            {
                lock (readWriteLock)
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = StickerCategory.STICKERS_DIR + "\\" + StickerCategory.HIGH_RESOLUTION_DIR + "\\" + sticker.Category + "\\" + sticker.Id;
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    int imageBytesCount = reader.ReadInt32();
                                    sticker.StickerImageBytes = reader.ReadBytes(imageBytesCount);
                                    return UI_Utils.Instance.createImageFromBytes(sticker.StickerImageBytes);
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
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
                                StickerCategory stickerCategory = new StickerCategory(category);
                                string[] files1 = store.GetFileNames(folder + "\\" + category + "\\*");
                                IEnumerable<string> files = files1.OrderBy(x => x);
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
                                                        stickerCategory._hasNewStickers = reader.ReadBoolean();
                                                    }
                                                    else
                                                    {
                                                        stickerCategory._listStickers.Add(new Sticker(category, stickerId, null, false));
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug.WriteLine("Exception in reading sticker file,message:" + ex.Message);
                                                }
                                            }
                                        }
                                    }
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

        public static void UpdateHasMoreMessages(string category, bool hasMoreStickers, bool hasNewMessages)
        {
            if (string.IsNullOrEmpty(category))
                return;

            if (HikeViewModel.stickerHelper != null && HikeViewModel.stickerHelper.GetStickersByCategory(category) != null)
            {
                StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(category);
                stickerCategory._hasMoreStickers = hasMoreStickers;
                stickerCategory._hasNewStickers = hasNewMessages;
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
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(hasMoreStickers);
                                writer.Write(showDownloadMessage);
                                writer.Write(hasNewMessages);
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

        public static void DeleteAllCategories()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string categoryFolder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR;
                        string[] folders = store.GetDirectoryNames(categoryFolder + "\\*");
                        if (folders != null)
                            foreach (string category in folders)
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
                                }
                            }
                        folders = store.GetDirectoryNames(categoryFolder + "\\*");
                        if (folders != null)
                        {
                            foreach (string category in folders)
                            {
                                string folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

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
                                }
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
    }
}
