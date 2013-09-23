using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace windows_client.utils
{
    public static class Extensions
    {
        public static bool NavigateToPage(this NavigationService source, string uri)
        {
            try
            {
                return source.Navigate(new Uri(uri, UriKind.Relative));
            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine("Extensions :: Navigation exception:" + e.StackTrace);
                return false;
            }
        }

        public static bool NavigateToPage(this NavigationService source, Uri uri)
        {
            try
            {
                return source.Navigate(uri);
            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine("Extensions :: Navigation exception:" + e.StackTrace);
                return false;
            }
        }
    }
}
