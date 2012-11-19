using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.Misc
{
    class Social
    {
        public static class FacebookSettings
        {
            public static string AppID = "437603502965046";
            public static string AppSecret = "35f194aebd97e1ce527997c457b5b0c5";
        }

        public static class FacebookAccess
        {
            public static string AccessToken { get; set; }
            public static string UserId { get; set; }
        }

    }
}
