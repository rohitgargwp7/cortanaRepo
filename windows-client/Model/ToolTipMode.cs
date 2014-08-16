using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.Model
{
    public enum ToolTipMode
    {
        /// <summary>
        /// No Action/ Equals NULL
        /// </summary>
        DEFAULT,

        /// <summary>
        /// Resetting chat in {0}. All hidden chats will be deleted.
        /// </summary>
        RESET_HIDDEN_MODE,

        /// <summary>
        /// Are you sure you want to reset hidden mode? All hidden chats will be deleted.
        /// </summary>
        RESET_HIDDEN_MODE_COMPLETED,

        /// <summary>
        /// hide your chats with a password. tap on logo to get started.
        /// </summary>
        HIDDEN_MODE_GETSTARTED,

        /// <summary>
        /// tap and hold a chat to mark it as ‘hidden’.
        /// </summary>
        HIDDEN_MODE_STEP2,

        /// <summary>
        /// to complete setup, tap on the shield to hide your chats.
        /// </summary>
        HIDDEN_MODE_COMPLETE,

        //Server Tips

        STICKERS,
        PROFILE,
        ATTACHMENTS,
        INFORMATIONAL,
        FAVOURITES,
        INVITE_FRIENDS,
        CHAT_THEMES,
        STATUS_UPDATE

    }
}
