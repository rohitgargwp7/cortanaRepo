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

        public JArray ReadByArray
        {
            get;
            set;
        }

        /// <summary>
        /// Last sent msg id in db
        /// </summary>
        public long LastSentMessageId
        {
            get;
            set;
        }

        /// <summary>
        /// Max msg id in bulk packet(for group less than equal to last sent msg id, for 1:1 max msg id)
        /// </summary>
        public long MaxReadById
        {
            get;
            set;
        }

        public MsisdnBulkData(string msisdn)
        {
            _msisdn = msisdn;
        }
    }
}
