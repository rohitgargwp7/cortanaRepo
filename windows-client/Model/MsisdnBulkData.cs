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

        private long _lastReadMsgId;
        public long LastReadMsgId
        {
            get { return _lastReadMsgId; }
            set { _lastReadMsgId = value; }
        }

        private JArray _readByArray;

        public JArray ReadByArray
        {
            get
            {
                if (_readByArray == null)
                    _readByArray = new JArray();
                return _readByArray;
            }
            set
            {
                if (value != _readByArray)
                    _readByArray = value;
            }
        }



        public MsisdnBulkData(string msisdn)
        {
            _msisdn = msisdn;
        }
    }
}
