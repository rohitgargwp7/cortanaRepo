using System.Data.Linq;

namespace CommonLibrary.Model
{
    public class HikeChatsDb : DataContext
    {
        public HikeChatsDb(string connectionString)
            : base(connectionString)
        { }

        public Table<ConvMessage> messages;

        public Table<GroupInfo> groupInfo;

        public Table<StatusMessage> statusMessage;
    }

    public class HikeUsersDb : DataContext
    {
        public HikeUsersDb(string connectionString)
            : base(connectionString)
        { }

        public Table<ContactInfo> users;

        public Table<Blocked> blockedUsersTable;

    }

    public class HikeMqttPersistenceDb : DataContext
    {
        public HikeMqttPersistenceDb(string connectionString)
            : base(connectionString)
        { }

        public Table<HikePacket> mqttMessages;
    }
}
