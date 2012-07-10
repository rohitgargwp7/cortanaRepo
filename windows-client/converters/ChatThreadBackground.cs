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
                return "#f2f2f2";
            }
            else if (ConvMessage.ChatBubbleType.HIKE_SENT == bubbleType)
            {
                return "#d4edfc";
            }
            else
            {
                return "#cff3cc";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
