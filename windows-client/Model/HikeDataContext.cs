using System.Data.Linq;

namespace windows_client.Model
{
    public class HikeChatsDb : DataContext
    {
        public HikeChatsDb(string connectionString)
            : base(connectionString)
        { }

        public Table<ConvMessage> messages;

        public Table<Conversation> conversations;

    }

    public class HikeUsersDb : DataContext
    {
        public HikeUsersDb(string connectionString)
            : base(connectionString)
        { }

        public Table<ContactInfo> users;

        public Table<Blocked> blockedUsersTable;

        public Table<Thumbnails> thumbnails;

    }

    public class HikeMqttPersistenceDb : DataContext
    {
        public HikeMqttPersistenceDb(string connectionString)
            : base(connectionString)
        { }

        public Table<HikePacket> mqttMessages;
    }
}
