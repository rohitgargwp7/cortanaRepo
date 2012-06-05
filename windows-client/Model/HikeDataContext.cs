using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
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

        public Table<HikeMqttPersistence> mqttMessages;
        //public Table<Thumbnails> thumbnails;
    }
}
