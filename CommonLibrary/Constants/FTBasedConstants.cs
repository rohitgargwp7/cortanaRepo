using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Constants
{
    public class FTBasedConstants
    {
        public static readonly string FILE_TRANSFER_LOCATION = "TransferredFiles";
        public static readonly string FILE_TRANSFER_TEMP_LOCATION = "TempTransferredFiles";
        public static readonly string FILES_BYTE_LOCATION = FILE_TRANSFER_LOCATION + "/FileBytes";
        public static readonly string SHARED_FILE_LOCATION = "/shared/transfers";
        public static readonly string FILES_THUMBNAILS = FILE_TRANSFER_LOCATION + "/Thumbnails";
        public static readonly string FILES_ATTACHMENT = FILE_TRANSFER_LOCATION + "/Attachments";
        public static readonly string TEMP_VIDEO_RECORDED = FILE_TRANSFER_LOCATION + "/TempVideo";
        public static readonly string FULL_VIEW_IMAGE_PREFIX = "_fullView";
        
        public static readonly string LOCATION = "location";
        public static readonly string VIDEO = "video";
        public static readonly string AUDIO = "audio";
        public static readonly string IMAGE = "image";
        public static readonly string CONTACT = "contact";
        public static readonly string UNKNOWN_FILE = "file";
        public static readonly string CT_CONTACT = "contact/share";
        public static readonly string POKE = "poke";

        public static readonly int APP_MIN_FREE_SIZE = 20971520;
        public static readonly int FILE_MAX_SIZE = 26214400;//in bytes

        public static readonly string IfModifiedSince = "30";
    }
}
