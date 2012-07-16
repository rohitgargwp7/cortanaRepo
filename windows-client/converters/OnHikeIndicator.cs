using System;
using System.Windows.Media.Imaging;
using System.Windows.Data;

namespace windows_client.converters
{
    public class OnHikeIndicator : IValueConverter
    {
        private static BitmapImage onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
        private static BitmapImage notOnHikeImage = new BitmapImage(new Uri("/View/images/ic_sms_user.png", UriKind.Relative));
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool onHike = (bool)value;
            if (onHike)
            {
                return onHikeImage;
            }
            return notOnHikeImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }
}
