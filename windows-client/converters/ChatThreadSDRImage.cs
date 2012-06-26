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
    public class ChatThreadSDRImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConvMessage.State msgState = (ConvMessage.State)value;
            switch (msgState)
            {
                case ConvMessage.State.SENT_CONFIRMED: return "images\\ic_sent.png";
                case ConvMessage.State.SENT_DELIVERED: return "images\\ic_delivered.png";
                case ConvMessage.State.SENT_DELIVERED_READ: return "images\\ic_read.png";
                default: return "images\\ic_tc.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
