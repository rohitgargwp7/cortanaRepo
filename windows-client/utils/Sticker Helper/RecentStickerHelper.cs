using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using windows_client.Model.Sticker;
using windows_client.ViewModel;
using CommonLibrary.Lib;

namespace windows_client.utils.Sticker_Helper
{
    public class RecentStickerHelper
    {
        public const string RECENTS_FILE = "recents";
        private const int maxStickersCount = 30;
        private static object readWriteLock = new object();

        public List<StickerObj> RecentStickers;

        public RecentStickerHelper()
        {
            RecentStickers = new List<StickerObj>();
        }

        public void LoadRecentStickers()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string fileName = StickerHelper.STICKERS_DIR + "\\" + RECENTS_FILE;
                    if (store.FileExists(fileName))
                    {
                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                int total = reader.ReadInt32();

                                for (int i = 0; i < total; i++)
                                {
                                    int count = reader.ReadInt32();
                                    string stickerId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    count = reader.ReadInt32();
                                    string category = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    RecentStickers.Add(new StickerObj(category, stickerId, null, false));
                                }

                            }
                        }
                    }

                }
            }
        }

        public void AddSticker(StickerObj currentSticker)
        {
            if (currentSticker == null)
                return;

            lock (readWriteLock)
            {
                if (!RecentStickers.Remove(currentSticker))
                    ShrinkToSize();

                RecentStickers.Insert(0, currentSticker);
                
                UpdateRecentsFile();
            }
        }

        public async Task UpdateRecentsFile()
        {
            try
            {
                await Task.Delay(1);

                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(StickerHelper.STICKERS_DIR))
                        store.CreateDirectory(StickerHelper.STICKERS_DIR);

                    string fileName = StickerHelper.STICKERS_DIR + "\\" + RECENTS_FILE;

                    try
                    {
                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RecentStickerHelper :: UpdateRecentsFile : DeletingFile , Exception : " + ex.StackTrace);
                    }

                    try
                    {
                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(RecentStickers.Count);
                            
                                foreach (StickerObj sticker in RecentStickers)
                                {
                                    writer.WriteStringBytes(sticker.Id);
                                    writer.WriteStringBytes(sticker.Category);
                                }
                                
                                writer.Flush();
                                writer.Close();
                            }

                            file.Close();
                            file.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RecentStickerHelper :: UpdateRecentsFile : WritingFile , Exception : " + ex.StackTrace);
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RecentStickerHelper :: UpdateRecentsFile , Exception : " + ex.StackTrace);
            }
        }

        private void ShrinkToSize()
        {
            if (this.RecentStickers.Count > (maxStickersCount - 1))
                RecentStickers.RemoveAt(RecentStickers.Count - 1);
        }

        public static void DeleteSticker(string categoryId, List<string> listStickers)
        {
            RecentStickerHelper recentSticker;

            if (HikeViewModel.StickerHelper == null || HikeViewModel.StickerHelper.RecentStickerHelper == null)
            {
                recentSticker = new RecentStickerHelper();
                recentSticker.LoadRecentStickers();
            }
            else
                recentSticker = HikeViewModel.StickerHelper.RecentStickerHelper;
            
            bool isStickerInRecent = false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (string stickerId in listStickers)
                {
                    if (recentSticker.RecentStickers.Remove(new StickerObj(categoryId, stickerId, null, false)))
                        isStickerInRecent = true;
                }

                if (isStickerInRecent)
                    recentSticker.UpdateRecentsFile();
            });
        }

        public static void DeleteCategory(string category)
        {
            RecentStickerHelper recentSticker;

            if (HikeViewModel.StickerHelper == null || HikeViewModel.StickerHelper.RecentStickerHelper == null)
            {
                recentSticker = new RecentStickerHelper();
                recentSticker.LoadRecentStickers();
            }
            else
                recentSticker = HikeViewModel.StickerHelper.RecentStickerHelper;

            bool isStickerInRecent = false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                for (int i = recentSticker.RecentStickers.Count - 1; i >= 0; i--)
                {
                    if (recentSticker.RecentStickers[i].Category == category)
                    {
                        recentSticker.RecentStickers.RemoveAt(i);
                        isStickerInRecent = true;
                    }
                }
                
                if (isStickerInRecent)
                    recentSticker.UpdateRecentsFile();
            });

        }

        public static void DeleteRecents()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        try
                        {
                            string filename = StickerHelper.STICKERS_DIR + "\\" + RECENTS_FILE;

                            if (store.FileExists(filename))
                                store.DeleteFile(filename);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("RecentStickerHelper :: DeleteRecents :Delete FIle: Exception :{0} , StackTrace:{1} ", ex.Message, ex.StackTrace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RecentStickerHelper :: DeleteRecents , Exception :{0} , StackTrace:{1} ", ex.Message, ex.StackTrace);
                }
            }
        }

    }
}
