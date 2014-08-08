using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using windows_client.ViewModel;

namespace windows_client.Model.Sticker
{
    public class StickerObj : IBinarySerializable,INotifyPropertyChanged
    {
        private string _id;
        private string _category;
        private bool _isHighRes;
        private byte[] _stickerImageBytes;
        public StickerObj(string category, string id, byte[] stickerImageBytes, bool isHighRes)
        {
            this._category = category;
            this._id = id;
            this._stickerImageBytes = stickerImageBytes;
            this._isHighRes = isHighRes;
        }

        public string Id
        {
            get
            {
                return _id;
            }
        }

        public string Category
        {
            get
            {
                return _category;
            }
        }
        public bool IsStickerDownloaded
        {
            get;
            set;
        }

        public byte[] StickerImageBytes
        {
            get
            {
                return _stickerImageBytes;
            }
            set
            {
                _stickerImageBytes = value;
            }
        }

        public BitmapImage StickerImage
        {
            get
            {
                if (_isHighRes)
                {
                    BitmapImage stickerImage = App.newChatThreadPage != null ? App.newChatThreadPage.lruStickerCache.GetObject(_category + "_" + Id) : null;
                    if (stickerImage == null)
                    {
                        stickerImage = new BitmapImage();
                        HikeViewModel.StickerHelper.GetSticker(stickerImage, _category, _id, _stickerImageBytes, true);
                    }

                    return stickerImage;
                }
                else
                {
                    BitmapImage stickerImage = HikeViewModel.StickerHelper.lruStickers.GetObject(_category + "_" + Id);
                    if (stickerImage == null)
                    {
                        stickerImage = new BitmapImage();
                        HikeViewModel.StickerHelper.GetSticker(stickerImage, _category, _id, _stickerImageBytes, false);
                    }

                    return stickerImage;
                }
            }
        }

        public void Write(BinaryWriter writer)
        {
        }
        public void Read(BinaryReader reader)
        {
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StickerObj))
                return false;

            StickerObj compareTo = obj as StickerObj;
            if (_id == compareTo._id && _category == compareTo._category && _isHighRes == compareTo._isHighRes)
                return true;
            return base.Equals(obj);
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StickerObj :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                    }
                });
            }
        }

        #endregion
    }
}
