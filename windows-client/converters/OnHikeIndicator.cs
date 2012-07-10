using System;
using System.Windows.Media.Imaging;
using System.Windows.Data;

namespace windows_client.converters
{
    public class OnHikeIndicator : IValueConverter
    {
        private static BitmapImage onHikeImage;
        private static BitmapImage notOnHikeImage;
        
        public OnHikeIndicator()
        {
            if (onHikeImage == null)
            {
                Uri uri = new Uri("/View/images/ic_hike_user.png", UriKind.Relative);
                onHikeImage = new BitmapImage(uri);
            }
            if (notOnHikeImage == null)
            {
                Uri uri = new Uri("/View/images/ic_sms_user.png", UriKind.Relative);
                notOnHikeImage = new BitmapImage(uri);
            }
        }

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
