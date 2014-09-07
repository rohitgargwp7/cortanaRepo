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

        private long _lastSentMsgId;

        public long LastSentMsgId
        {
            get { return _lastSentMsgId; }
            set { _lastSentMsgId = value; }
        }

        public MsisdnBulkData(string msisdn)
        {
            _msisdn = msisdn;
        }
    }
}
