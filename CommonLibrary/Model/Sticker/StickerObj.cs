namespace CommonLibrary.Model.Sticker
{
    public class StickerObj
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

        public override bool Equals(object obj)
        {
            if (!(obj is StickerObj))
                return false;

            StickerObj compareTo = obj as StickerObj;
            
            if (_id == compareTo._id && _category == compareTo._category && _isHighRes == compareTo._isHighRes)
                return true;

            return base.Equals(obj);
        }
    }
}
