using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.Model
{
    class TypingNotification
    {
        object[] _params;

        public TypingNotification(object[] paramaters)
        {
            _params = paramaters;
        }

        public void AutoHideAfterTyping()
        {
            App.ViewModel.CallAutohide(_params);
        }

    }
}
