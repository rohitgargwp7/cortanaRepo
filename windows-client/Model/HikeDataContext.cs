using System.Data.Linq;

namespace windows_client.Model
{
    public class HikeDataContext : DataContext
    {
        public HikeDataContext(string connectionString)
            : base(connectionString)
        { }

        public Table<ConvMessage> messages;

        public Table<Conversation> conversations;

        public Table<ContactInfo> users;

        public Table<Blocked> blockedUsersTable;

        public Table<HikePacket> mqttMessages;
        
        public Table<Thumbnails> thumbnails;
    }
}
