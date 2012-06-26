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
using System.Windows.Data;
using windows_client.Model;

namespace windows_client.converters
{
    public class ChatThreadSDRImageShow : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConvMessage.State msgState = (ConvMessage.State)value;
            if (ConvMessage.State.SENT_UNCONFIRMED == msgState || ConvMessage.State.UNKNOWN == msgState || ConvMessage.State.RECEIVED_READ == msgState || ConvMessage.State.RECEIVED_UNREAD == msgState)
            {
                return "Collapsed";
            }

            return "Visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
