﻿using System;
using System.Windows.Data;

namespace windows_client.Converters
{
    public class InvitedStringVisiblity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isInvited = (bool)value;
            if (isInvited)
            {
                return "visible";
            }
            return "collapsed";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}