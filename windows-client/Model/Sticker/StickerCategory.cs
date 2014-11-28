using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using windows_client.Model.Sticker;
using windows_client.utils;
using windows_client.ViewModel;

namespace windows_client.Model.Sticker
{
    public class StickerCategory : INotifyPropertyChanged
    {
        private string _category;
        private bool _hasMoreStickers = true;
        private bool _showDownloadMessage = true;
        private bool _hasNewStickers = false;
        private ObservableCollection<StickerObj> _listStickers;
        private bool _isDownLoading;
        private bool _isSelected;
        private string _overlayBackground;
        private string _overlayText;
        private bool _isVisibile;
        private bool _isRemoved;


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

        public bool IsVisbile
        {
            get
            {
                return _isVisibile;
            }
            set
            {
                _isVisibile = value;
                NotifyPropertyChanged("IsVisbile");
                NotifyPropertyChanged("StickerShopIcon");
            }
        }

        public bool IsRemoved
        {
            get { return _isRemoved; }
            set { _isRemoved = value; }
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
                NotifyPropertyChanged("StickersAvailableVisibility");
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
                NotifyPropertyChanged("StickersAvailableVisibility");
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("CategoryIcon");
                }
            }
        }
        public BitmapImage CategoryIcon
        {
            get
            {
                switch (_category)
                {
                    case StickerHelper.CATEGORY_RECENT:
                        return _isSelected ? UI_Utils.Instance.RecentIconActive : UI_Utils.Instance.RecentIconInActive;
                    case StickerHelper.CATEGORY_HUMANOID:
                        return _isSelected ? UI_Utils.Instance.HumanoidActive : UI_Utils.Instance.HumanoidInactive;
                    case StickerHelper.CATEGORY_DOGGY:
                        return _isSelected ? UI_Utils.Instance.DoggyActive : UI_Utils.Instance.DoggyInactive;
                    case StickerHelper.CATEGORY_KITTY:
                        return _isSelected ? UI_Utils.Instance.KittyActive : UI_Utils.Instance.KittyInactive;
                    case StickerHelper.CATEGORY_EXPRESSIONS:
                        return _isSelected ? UI_Utils.Instance.ExpressionsActive : UI_Utils.Instance.ExpressionsInactive;
                    case StickerHelper.CATEGORY_BOLLYWOOD:
                        return _isSelected ? UI_Utils.Instance.BollywoodActive : UI_Utils.Instance.BollywoodInactive;
                    case StickerHelper.CATEGORY_TROLL:
                        return _isSelected ? UI_Utils.Instance.TrollActive : UI_Utils.Instance.TrollInactive;
                    case StickerHelper.CATEGORY_HUMANOID2:
                        return _isSelected ? UI_Utils.Instance.Humanoid2Active : UI_Utils.Instance.Humanoid2Inactive;
                    case StickerHelper.CATEGORY_AVATARS:
                        return _isSelected ? UI_Utils.Instance.AvatarsActive : UI_Utils.Instance.AvatarsInactive;
                    case StickerHelper.CATEGORY_INDIANS:
                        return _isSelected ? UI_Utils.Instance.IndianActive : UI_Utils.Instance.IndianInactive;
                    case StickerHelper.CATEGORY_JELLY:
                        return _isSelected ? UI_Utils.Instance.JellyActive : UI_Utils.Instance.JellyInactive;
                    case StickerHelper.CATEGORY_SPORTS:
                        return _isSelected ? UI_Utils.Instance.SportsActive : UI_Utils.Instance.SportsInactive;
                    case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                        return _isSelected ? UI_Utils.Instance.SmileyExpressionsActive : UI_Utils.Instance.SmileyExpressionsInactive;
                    case StickerHelper.CATEGORY_LOVE:
                        return _isSelected ? UI_Utils.Instance.LoveActive : UI_Utils.Instance.LoveInactive;
                    case StickerHelper.CATEGORY_DELHI:
                        return _isSelected ? UI_Utils.Instance.DelhiActive : UI_Utils.Instance.DelhiInactive;
                    case StickerHelper.CATEGORY_MUMBAI:
                        return _isSelected ? UI_Utils.Instance.MumbaiActive : UI_Utils.Instance.MumbaiInactive;
                    case StickerHelper.CATEGORY_GUJARAT:
                        return _isSelected ? UI_Utils.Instance.GujaratActive : UI_Utils.Instance.GujaratInactive;
                    case StickerHelper.CATEGORY_BANGALORE:
                        return _isSelected ? UI_Utils.Instance.BangaloreActive : UI_Utils.Instance.BangaloreInactive;
                    case StickerHelper.CATEGORY_HYDERABAD:
                        return _isSelected ? UI_Utils.Instance.HyderabadActive : UI_Utils.Instance.HyderabadInactive;
                    case StickerHelper.CATEGORY_BHOPAL:
                        return _isSelected ? UI_Utils.Instance.BhopalActive : UI_Utils.Instance.BhopalInactive;
                    case StickerHelper.CATEGORY_CHENNAI:
                        return _isSelected ? UI_Utils.Instance.ChennaiActive : UI_Utils.Instance.ChennaiInactive;
                    case StickerHelper.CATEGORY_KERALA:
                        return _isSelected ? UI_Utils.Instance.KeralaActive : UI_Utils.Instance.KeralaInactive;
                    case StickerHelper.CATEGORY_KOLKATA:
                        return _isSelected ? UI_Utils.Instance.KolkataActive : UI_Utils.Instance.KolkataInactive;
                    case StickerHelper.CATEGORY_BIHAR:
                        return _isSelected ? UI_Utils.Instance.BiharActive : UI_Utils.Instance.BiharInactive;
                    case StickerHelper.CATEGORY_GUWAHATI:
                        return _isSelected ? UI_Utils.Instance.GuwahatiActive : UI_Utils.Instance.GuwahatiInactive;
                    default:
                        return new BitmapImage();
                }
            }
        }

        public BitmapImage StickerShopIcon
        {
            get
            {
                switch (_category)
                {
                    case StickerHelper.CATEGORY_HUMANOID:
                        return UI_Utils.Instance.HumanoidShopIcon;
                    case StickerHelper.CATEGORY_DOGGY:
                        return UI_Utils.Instance.DoggyShopIcon;
                    case StickerHelper.CATEGORY_KITTY:
                        return UI_Utils.Instance.KittyShopIcon;
                    case StickerHelper.CATEGORY_EXPRESSIONS:
                        return UI_Utils.Instance.ExpressionsShopIcon;
                    case StickerHelper.CATEGORY_BOLLYWOOD:
                        return UI_Utils.Instance.BollywoodShopIcon;
                    case StickerHelper.CATEGORY_TROLL:
                        return UI_Utils.Instance.TrollShopIcon;
                    case StickerHelper.CATEGORY_HUMANOID2:
                        return UI_Utils.Instance.Humanoid2ShopIcon;
                    case StickerHelper.CATEGORY_AVATARS:
                        return UI_Utils.Instance.AvatarsShopIcon;
                    case StickerHelper.CATEGORY_INDIANS:
                        return UI_Utils.Instance.IndianShopIcon;
                    case StickerHelper.CATEGORY_JELLY:
                        return UI_Utils.Instance.JellyShopIcon;
                    case StickerHelper.CATEGORY_SPORTS:
                        return UI_Utils.Instance.SportsShopIcon;
                    case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                        return UI_Utils.Instance.SmileyExpressionsShopIcon;
                    case StickerHelper.CATEGORY_LOVE:
                        return UI_Utils.Instance.LoveShopIcon;
                    case StickerHelper.CATEGORY_DELHI:
                        return UI_Utils.Instance.DelhiShopIcon;
                    case StickerHelper.CATEGORY_MUMBAI:
                        return UI_Utils.Instance.MumbaiShopIcon;
                    case StickerHelper.CATEGORY_GUJARAT:
                        return UI_Utils.Instance.GujaratShopIcon;
                    case StickerHelper.CATEGORY_BANGALORE:
                        return UI_Utils.Instance.BangaloreShopIcon;
                    case StickerHelper.CATEGORY_HYDERABAD:
                        return UI_Utils.Instance.HyderabadShopIcon;
                    case StickerHelper.CATEGORY_KERALA:
                        return UI_Utils.Instance.KeralaShopIcon;
                    case StickerHelper.CATEGORY_BHOPAL:
                        return UI_Utils.Instance.BhopalShopIcon;
                    case StickerHelper.CATEGORY_CHENNAI:
                        return UI_Utils.Instance.ChennaiShopIcon;
                    case StickerHelper.CATEGORY_KOLKATA:
                        return UI_Utils.Instance.KolkataShopIcon;
                    case StickerHelper.CATEGORY_BIHAR:
                        return UI_Utils.Instance.BiharShopIcon;
                    case StickerHelper.CATEGORY_GUWAHATI:
                        return UI_Utils.Instance.GuwahatiShopIcon;//todo:change
                    default:
                        return new BitmapImage();
                }
            }
        }

        public Visibility  StickersAvailableVisibility
        {
            get
            {
                return (_hasNewStickers || _hasMoreStickers) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ObservableCollection<StickerObj> ListStickers
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
            _listStickers = new ObservableCollection<StickerObj>();
        }

        #region Overlay Properties
        SolidColorBrush _overlayBackgroundColor;
        public SolidColorBrush OverlayBackgroundColor
        {
            get
            {
                if (_overlayBackgroundColor == null)
                    _overlayBackgroundColor = UI_Utils.Instance.ConvertStringToColor(_overlayBackground ?? StickerHelper.GetOverLayColor(_category));

                return _overlayBackgroundColor;
            }
        }

        public string OverlayText
        {
            get
            {
                return _overlayText ?? StickerHelper.GetOverLayText(_category);
            }
            set
            {
                _overlayText = value;
            }
        }

        private string _stickerCount;
        public string StickerCount
        {
            get
            {
                if (_stickerCount == null)
                {
                    int count = StickerHelper.GetStickersCount(_category);
                    if (ListStickers.Count > count)
                        count = ListStickers.Count;

                    _stickerCount = string.Format(HikeConstants.Stickers.STICKER_COUNT_TEXT, count);
                }
                return _stickerCount;
            }
        }

        public bool IsCheckboxEnabled
        {
            get
            {
                if (_category == StickerHelper.CATEGORY_HUMANOID || _category == StickerHelper.CATEGORY_EXPRESSIONS)
                    return false;
                return true;
            }
        }
        public BitmapImage OverlayImage
        {
            get
            {
                switch (_category)
                {
                    case StickerHelper.CATEGORY_HUMANOID:
                        return UI_Utils.Instance.HumanoidOverlay;
                    case StickerHelper.CATEGORY_EXPRESSIONS:
                        return UI_Utils.Instance.ExpressionsOverlay;
                    case StickerHelper.CATEGORY_DOGGY:
                        return UI_Utils.Instance.DoggyOverlay;
                    case StickerHelper.CATEGORY_KITTY:
                        return UI_Utils.Instance.KittyOverlay;
                    case StickerHelper.CATEGORY_BOLLYWOOD:
                        return UI_Utils.Instance.BollywoodOverlay;
                    case StickerHelper.CATEGORY_TROLL:
                        return UI_Utils.Instance.TrollOverlay;
                    case StickerHelper.CATEGORY_HUMANOID2:
                        return UI_Utils.Instance.Humanoid2Overlay;
                    case StickerHelper.CATEGORY_AVATARS:
                        return UI_Utils.Instance.AvatarsOverlay;
                    case StickerHelper.CATEGORY_INDIANS:
                        return UI_Utils.Instance.IndiansOverlay;
                    case StickerHelper.CATEGORY_JELLY:
                        return UI_Utils.Instance.JellyOverlay;
                    case StickerHelper.CATEGORY_SPORTS:
                        return UI_Utils.Instance.SportsOverlay;
                    case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                        return UI_Utils.Instance.SmileyExpressionsOverlay;
                    case StickerHelper.CATEGORY_LOVE:
                        return UI_Utils.Instance.LoveOverlay;
                    case StickerHelper.CATEGORY_DELHI:
                        return UI_Utils.Instance.DelhiOverlay;
                    case StickerHelper.CATEGORY_MUMBAI:
                        return UI_Utils.Instance.MumbaiOverlay;
                    case StickerHelper.CATEGORY_GUJARAT:
                        return UI_Utils.Instance.GujaratOverlay;
                    case StickerHelper.CATEGORY_BANGALORE:
                        return UI_Utils.Instance.BangaloreOverlay;
                    case StickerHelper.CATEGORY_HYDERABAD:
                        return UI_Utils.Instance.HyderabadOverlay;
                    case StickerHelper.CATEGORY_BHOPAL:
                        return UI_Utils.Instance.BhopalOverlay;
                    case StickerHelper.CATEGORY_CHENNAI:
                        return UI_Utils.Instance.ChennaiOverlay;
                    case StickerHelper.CATEGORY_KERALA:
                        return UI_Utils.Instance.KerelaOverlay;
                    case StickerHelper.CATEGORY_KOLKATA:
                        return UI_Utils.Instance.KolkataOverlay;
                    case StickerHelper.CATEGORY_BIHAR:
                        return UI_Utils.Instance.BiharOverlay;
                    case StickerHelper.CATEGORY_GUWAHATI:
                        return UI_Utils.Instance.GuwahatiOverlay;
                }
                return null;
            }
        }

        public string OverlayBackgroundColorString
        {
            set
            {
                _overlayBackground = value;
            }
            get
            {
                return _overlayBackground;
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StickerCategory :: NotifyPropertyChanged : NotifyPropertyChanged, Exception : " + ex.StackTrace);
                    }
                });
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
                        string folder = StickerHelper.STICKERS_DIR + "\\" + StickerHelper.HIGH_RESOLUTION_DIR + "\\" + _category;
                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (!store.DirectoryExists(StickerHelper.STICKERS_DIR))
                            {
                                store.CreateDirectory(StickerHelper.STICKERS_DIR);
                            }
                            if (!store.DirectoryExists(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.HIGH_RESOLUTION_DIR))
                            {
                                store.CreateDirectory(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.HIGH_RESOLUTION_DIR);
                            }
                            if (!store.DirectoryExists(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.HIGH_RESOLUTION_DIR + "\\" + _category))
                            {
                                store.CreateDirectory(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.HIGH_RESOLUTION_DIR + "\\" + _category);
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
                        string folder = StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR + "\\" + _category;

                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (!store.DirectoryExists(StickerHelper.STICKERS_DIR))
                            {
                                store.CreateDirectory(StickerHelper.STICKERS_DIR);
                            }
                            if (!store.DirectoryExists(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR))
                            {
                                store.CreateDirectory(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR);
                            }
                            if (!store.DirectoryExists(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR + "\\" + _category))
                            {
                                store.CreateDirectory(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR + "\\" + _category);
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
                            string metadataFile = folder + "\\" + StickerHelper.METADATA;
                            using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    _hasMoreStickers = hasMoreStickers;
                                    this.Write(writer);
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
                    string folder = StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR + "\\" + _category;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(StickerHelper.STICKERS_DIR))
                        {
                            store.CreateDirectory(StickerHelper.STICKERS_DIR);
                        }
                        if (!store.DirectoryExists(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR))
                        {
                            store.CreateDirectory(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR);
                        }
                        if (!store.DirectoryExists(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR + "\\" + _category))
                        {
                            store.CreateDirectory(StickerHelper.STICKERS_DIR + "\\" + StickerHelper.LOW_RESOLUTION_DIR + "\\" + _category);
                        }
                        string metadataFile = folder + "\\" + StickerHelper.METADATA;
                        using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                this._showDownloadMessage = showDownloadMessage;
                                this.Write(writer);
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

        public void Write(BinaryWriter writer)
        {
            writer.Write(_hasMoreStickers);
            writer.Write(_showDownloadMessage);
            writer.Write(_hasNewStickers);
            writer.WriteString(OverlayText);
            writer.WriteString(_overlayBackground ?? StickerHelper.GetOverLayColor(_category));
            writer.Write(_isVisibile);
            writer.Write(_isRemoved);
        }

        public void Read(BinaryReader reader)
        {
            _hasMoreStickers = reader.ReadBoolean();
            _showDownloadMessage = reader.ReadBoolean();
            _hasNewStickers = reader.ReadBoolean();

            try
            {
                _overlayText = reader.ReadString();
                _overlayBackground = reader.ReadString();
                _isVisibile = reader.ReadBoolean();//works because different file for different category
                _isRemoved = reader.ReadBoolean();
            }
            catch
            {
            }
        }

    }
}
