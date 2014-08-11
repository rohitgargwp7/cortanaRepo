using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.HikeConstants;
using windows_client.Controls;


namespace windows_client.ServerTips
{
    class TipInfoBase
    {
        public string Type { get; set; }
        public string HeadText { get; set; }
        public string BodyText { get; set; }
        public string TipId { get; set; }

        public TipInfoBase(string type, string header, string body, string id)
        {
            Type=type;
            HeadText = header;
            BodyText = body;
            TipId = id;
        }
    }
}
