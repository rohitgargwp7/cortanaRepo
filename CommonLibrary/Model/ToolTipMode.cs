using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Model
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

        #region Server Tips
        
        /// <summary>
        /// Tap and check new stickers
        /// </summary>
        STICKERS,
        
        /// <summary>
        /// Its Photo Time. Add your photo.
        /// </summary>
        PROFILE_PIC,

        /// <summary>
        /// tap and send files to friends
        /// </summary>
        ATTACHMENTS,

        /// <summary>
        /// here is some information for you
        /// </summary>
        INFORMATIONAL,

        /// <summary>
        /// your favourites can see your last seen
        /// </summary>
        FAVOURITES,

        /// <summary>
        /// invite friends to hike and earn money
        /// </summary>
        INVITE_FRIENDS,

        /// <summary>
        /// hey check out new chat themes
        /// </summary>
        CHAT_THEMES,

        /// <summary>
        /// there is a status update
        /// </summary>
        STATUS_UPDATE

        #endregion
    }
}
