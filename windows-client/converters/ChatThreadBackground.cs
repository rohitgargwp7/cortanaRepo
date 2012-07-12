using System;
using System.Windows.Data;
using windows_client.Model;

namespace windows_client.converters
{
    public class ChatThreadBackground : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConvMessage.ChatBubbleType bubbleType = (ConvMessage.ChatBubbleType)value;


            if (ConvMessage.ChatBubbleType.RECEIVED == bubbleType)
            {
                return "#eeeeec";
            }
            else if (ConvMessage.ChatBubbleType.HIKE_SENT == bubbleType)
            {
                return "#ccecfe";
            }
            else
            {
                return "#e1f4d7";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
