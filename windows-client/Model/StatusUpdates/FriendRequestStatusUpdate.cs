using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Languages;

namespace windows_client.Model
{
    public class FriendRequestStatusUpdate : BaseStatusUpdate
    {
        public FriendRequestStatusUpdate(ConversationListObject c)
            : base(c, string.Empty)
        {
            Text = AppResources.Profile_DoTheSame;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;

            BaseStatusUpdate status = (BaseStatusUpdate)obj;
            if (string.IsNullOrEmpty(this.ServerId) || string.IsNullOrEmpty(status.ServerId))
            {
                return this.Msisdn.Equals(status.Msisdn);
            }
            return this.ServerId.Equals(status.ServerId);
        }
    }
}
