using System;
using System.Windows.Data;

namespace windows_client.converters
{
    public class ChatBubbleMargin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isSent = (bool)value;
            if (isSent)
            {
                return "15,0,10,10";
            }
            return "5,0,10,10";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

