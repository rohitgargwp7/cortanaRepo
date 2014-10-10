using CommonLibrary.Lib;
using System.Collections.Generic;
using System.IO;

namespace CommonLibrary.Model.Sticker
{
    public class StickerCategory
    {
        private string _category;
        private bool _hasMoreStickers = true;
        private bool _showDownloadMessage = true;
        private bool _hasNewStickers = false;
        private List<StickerObj> _listStickers;
        private string _overlayBackground;
        private string _overlayText;
        private static object readWriteLock = new object();

        public string Category
        {
            get
            {
                return _category;
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

        public List<StickerObj> ListStickers
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
            _listStickers = new List<StickerObj>();
        }

        public string OverlayText
        {
            get
            {
                return _overlayText;
            }
            set
            {
                _overlayText = value;
            }
        }

        public string OverlayBackgroundColorString
        {
            set
            {
                _overlayBackground = value;
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_hasMoreStickers);
            writer.Write(_showDownloadMessage);
            writer.Write(_hasNewStickers);
            writer.WriteString(_overlayText);
            writer.WriteString(_overlayBackground);
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
            }
            catch
            {
            }
        }
    }
}
