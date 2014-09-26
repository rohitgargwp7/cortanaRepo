using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace windows_client.utils.ThemeManager
{
    public class ThemeResourceDictionary : ResourceDictionary
    {
        private ResourceDictionary lightResources;
        private ResourceDictionary darkResources;

        public ResourceDictionary DarkResources
        {
            get { return darkResources; }
            set
            {
                darkResources = value;

                if (IsDarkTheme && value != null)
                {
                    MergedDictionaries.Add(value);
                }
            }
        }
 
        public ResourceDictionary LightResources
        {
            get { return lightResources; }
            set
            {
                lightResources = value;

                if (!IsDarkTheme && value != null)
                {
                    MergedDictionaries.Add(value);
                }
            }
        }

        private bool IsDarkTheme
        {
            get
            {
                return HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.BLACK_THEME);
            }
        }
    }
}
