using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.Model
{
    [Table(Name = "messageHash")]
    public class MessageHash
    {
        int _messagehash;

        public MessageHash()
        {
        }

        public MessageHash(int messageHash)
        {
            _messagehash = messageHash;
        }

        [Column(IsPrimaryKey=true)]
        public int Messagehash
        {
            get
            {
                return _messagehash;
            }
            set
            {
                if (value != _messagehash)
                    _messagehash = value;
            }
        }
    }
}
