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

namespace windows_client.db
{
    public class DBConstants
    {
        public static readonly int CONVERSATIONS_DATABASE_VERSION = 1;

        public static readonly int USERS_DATABASE_VERSION = 2;

        public static readonly String HAS_CUSTOM_PHOTO = "hascustomphoto";

        public static readonly String CONVERSATIONS_METADATA = "metadata";

        public static readonly String CONVERSATIONS_DATABASE_NAME = "chats";

        public static readonly String CONVERSATIONS_TABLE = "conversations";

        public static readonly String MESSAGES_TABLE = "messages";

        public static readonly String USERS_DATABASE_NAME = "hikeusers";

        public static readonly String USERS_TABLE = "users";

        /* Table Constants */

        public static readonly String MESSAGE = "message";

        public static readonly String MSG_STATUS = "msgStatus";

        public static readonly String TIMESTAMP = "timestamp";

        public static readonly String MESSAGE_ID = "msgid";

        public static readonly String MAPPED_MSG_ID = "mappedMsgId";

        public static readonly String CONV_ID = "convid";

        public static readonly String ONHIKE = "onhike";

        public static readonly String CONTACT_ID = "contactid";

        public static readonly String MSISDN = "msisdn";

        public static readonly String CONVERSATION_INDEX = "conversation_idx";

        public static readonly String ID = "id";

        public static readonly String NAME = "name";

        public static readonly String PHONE = "phoneNumber";

        public static readonly String BLOCK_TABLE = "blocked";

        public static readonly String THUMBNAILS_TABLE = "thumbnails";

        public static readonly String IMAGE = "image";

    }
}
