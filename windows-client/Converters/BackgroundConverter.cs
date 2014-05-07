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
    public class BackgroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts from a tile size to the corresponding width.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush result = new SolidColorBrush(Color.FromArgb(255,0xdd,0xdd,0xdd));

            bool visible = System.Convert.ToBoolean(value);

            if (visible == true)
                return result = (SolidColorBrush)App.Current.Resources["HikeBlueHeader"];
            else
                return result;
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
