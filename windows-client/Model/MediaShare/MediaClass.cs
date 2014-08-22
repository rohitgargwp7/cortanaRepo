using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace windows_client.Model.MediaShare
{
    [DataContract]
    public class MediaClass
    {
        [DataMember]
        public DateTime TimeStamp { get; set; }

        public BitmapImage ThumbnailImage { get { } }
    }
}
