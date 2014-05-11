using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace windows_client.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts from a tile size to the corresponding width.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = System.Convert.ToBoolean(value);

            if (visible == true)
                return System.Windows.Visibility.Visible;
            else
                return System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Not used.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
