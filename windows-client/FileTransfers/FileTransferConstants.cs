using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.FileTransfers
{
    public enum ImageQuality : byte
    {
        Normal = 0,
        Better,
        UnCompressed
    }

    class FileTransferConstants
    {
        public static readonly int MAX_THUMBNAILSIZE = 4800;
        public static readonly int ATTACHMENT_THUMBNAIL_MAX_HEIGHT = 180;
        public static readonly int ATTACHMENT_THUMBNAIL_MAX_WIDTH = 180;
        public static readonly int ATTACHMENT_MAX_HEIGHT = 800;
        public static readonly int ATTACHMENT_MAX_WIDTH = 800;

        //IMAGE QUALITY FACTOR
        public static readonly int IMAGE_QUALITY_NORMAL = 65;
        public static readonly int IMAGE_QUALITY_BETTER = 80;
        public static readonly int IMAGE_QUALITY_BEST = 100;
    }
}
