using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace windows_client.utils
{
    public class StickerCategory
    {
        const string STICKERS_DIR = "stickers";
        string _category;
        bool _hasMoreStickers;
        public Dictionary<int, Sticker> dictStickers = new Dictionary<int, Sticker>();
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
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string FileName = STICKERS_DIR + "\\" + _category;
                        using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                _hasMoreStickers = reader.ReadBoolean();
                                int count = reader.ReadInt32();
                                for (int i = 0; i < count; i++)
                                {
                                    int id = reader.ReadInt32();
                                    int imageBytesCount = reader.ReadInt32();
                                    Byte[] imageBytes = reader.ReadBytes(imageBytesCount);
                                    dictStickers.Add(id, new Sticker(_category, id, UI_Utils.Instance.createImageFromBytes(imageBytes)));
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
        }

        public void WriteToFile(Dictionary<int, Byte[]> dictStcikers, bool hasMoreStickers)
        {
            if (dictStcikers != null && dictStcikers.Count > 0)
            {
                try
                {
                    string fileName = STICKERS_DIR + "\\" + _category;
                    _hasMoreStickers = hasMoreStickers;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(STICKERS_DIR))
                        {
                            store.CreateDirectory(STICKERS_DIR);
                        }
                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            if (file.Length > 0)
                            {
                                using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                {
                                    try
                                    {
                                        reader.ReadBoolean();
                                        int count = reader.ReadInt32();
                                        for (int i = 0; i < count; i++)
                                        {
                                            int id = reader.ReadInt32();
                                            int imageBytesCount = reader.ReadInt32();
                                            dictStcikers[id] = reader.ReadBytes(imageBytesCount);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
                                    }
                                }
                            }
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(hasMoreStickers);
                                writer.Write(dictStcikers.Count);
                                foreach (int stickerId in dictStcikers.Keys)
                                {
                                    writer.Write(stickerId);
                                    Byte[] imageBytes = dictStcikers[stickerId];
                                    writer.Write(imageBytes.Length);
                                    writer.Write(imageBytes);
                                }
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
}
