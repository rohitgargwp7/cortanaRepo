using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace windows_client.Converters
{
    public class TileSizeToWidthConverter : IValueConverter
    {
        /// <summary>
        /// Converts from a tile size to the corresponding width.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double baseWidth = 0;

            switch ((TileSize)value)
            {
                case TileSize.Default:
                    baseWidth = 150;
                    break;

                case TileSize.Small:
                    baseWidth = 99;
                    break;

                case TileSize.Medium:
                    baseWidth = 210;
                    break;

                case TileSize.Large:
                    baseWidth = 432;
                    break;
            }

            double multiplier;

            if (parameter == null || double.TryParse(parameter.ToString(), out multiplier) == false)
            {
                multiplier = 1;
            }

            return baseWidth * multiplier;
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
