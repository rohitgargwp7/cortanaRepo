using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.Model
{
    class MsisdnBulkData
    {
        private string _msisdn;
        public string Msisdn
        {
            get { return _msisdn; }
            set { _msisdn = value; }
        }

        private List<ConvMessage> _listMessages = new List<ConvMessage>();
        public List<ConvMessage> ListMessages
        {
            get { return _listMessages; }
            set { _listMessages = value; }
        }

        private long _lastDeliveredMsgId;
        public long LastDeliveredMsgId
        {
            get { return _lastDeliveredMsgId; }
            set { _lastDeliveredMsgId = value; }
        }

        private Dictionary<long, JArray> _dictReadBy = new Dictionary<long, JArray>();

        public Dictionary<long, JArray> DictReadBy
        {
            get { return _dictReadBy; }
            set { _dictReadBy = value; }
        }



        public MsisdnBulkData(string msisdn)
        {
            _msisdn = msisdn;
        }
    }
}
