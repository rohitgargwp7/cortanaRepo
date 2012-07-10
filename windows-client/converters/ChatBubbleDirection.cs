using System;
using System.Windows.Data;

namespace windows_client.converters
{
    public class ChatBubbleDirection : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isSent = (bool)value;
            if (isSent)
            {
                return "LowerRight";
            }
            return "LowerLeft";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    }

