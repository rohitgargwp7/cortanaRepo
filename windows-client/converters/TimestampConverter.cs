using System;
using System.Windows.Data;
using windows_client.utils;

namespace windows_client.converters
{
    public class TimestampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long timestamp = (long)value;
            return TimeUtils.getTimeString(timestamp);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }
}
