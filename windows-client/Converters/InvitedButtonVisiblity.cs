﻿using System;
using System.Windows.Data;

namespace windows_client.Converters
{
    public class InvitedButtonVisiblity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isInvited = (bool)value;
            if (isInvited)
            {
                return "collapsed";
            }
            return "visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }
}