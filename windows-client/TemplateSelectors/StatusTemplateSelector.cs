using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using windows_client.Controls;
using windows_client.Model;

namespace windows_client.TemplateSelectors
{
    public class StatusTemplateSelector : TemplateSelector
    {
        #region Properties

        public DataTemplate DTFriendRequest
        {
            get;
            set;
        }

        public DataTemplate DTImageStatus
        {
            get;
            set;
        }

        public DataTemplate DTProTip
        {
            get;
            set;
        }

        public DataTemplate DTTextStatus
        {
            get;
            set;
        }

        public DataTemplate DTDefaultStatus
        {
            get;
            set;
        }

        #endregion

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TextStatus)
                return DTTextStatus;
            else if (item is ImageStatus)
                return DTImageStatus;
            else if (item is FriendRequestStatusUpdate)
                return DTFriendRequest;
            else if (item is DefaultStatus)
                return DTDefaultStatus;
            else if (item is ProTipStatusUpdate)
                return DTProTip;
            else
                return (new DataTemplate());
        }
    }
}
