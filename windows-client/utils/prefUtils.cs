using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.IO;

namespace windows_client.utils
{
    public class prefUtils
    {
        public static T GetObject<T>(string key)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key))
            {
                T value;
                var settings = IsolatedStorageSettings.ApplicationSettings;
                if (settings.TryGetValue<T>(key, out value))
                {
                    return value;
                }
            }
            return default(T);
        }

        public static void savePreference<T>(string key, T objectToSave)
        {
            if(IsolatedStorageSettings.ApplicationSettings.Contains(key))
            {
                IsolatedStorageSettings.ApplicationSettings[key] = objectToSave;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings.Add(key, objectToSave);
            }
        }

        public static void DeleteObject(string key)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key))
            {
                IsolatedStorageSettings.ApplicationSettings.Remove(key);
            }
        }


    }
}
