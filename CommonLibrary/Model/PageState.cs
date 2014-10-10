using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Model
{
    public enum PageState
    {
        /// <summary>
        /// Welcome Screen
        /// </summary>
        WELCOME_SCREEN,

        /// <summary>
        /// Enter number Screen
        /// </summary>
        PHONE_SCREEN,

        /// <summary>
        /// Enter Pin Screen
        /// </summary>
        PIN_SCREEN,

        /// <summary>
        /// Status tutorial screen.
        /// </summary>
        TUTORIAL_SCREEN_STATUS,

        /// <summary>
        /// Sticker tutorial screen.
        /// </summary>
        TUTORIAL_SCREEN_STICKERS,

        /// <summary>
        /// Enter name screen.
        /// </summary>
        SETNAME_SCREEN,

        /// <summary>
        /// ConversationList Screen
        /// </summary>
        CONVLIST_SCREEN,

        /// <summary>
        /// Upgrade page screen
        /// </summary>
        UPGRADE_SCREEN,

        //the below pages have been removed after 2.2, but still we need these to handle them in case of upgrade.
        //app settings is storing pagestate in form of object and not value, hence while converting on upgrade
        //app settings is getting corrupted which throws the ap to welcome page
        WELCOME_HIKE_SCREEN,
        NUX_SCREEN_FRIENDS,
        NUX_SCREEN_FAMILY
    }
}
