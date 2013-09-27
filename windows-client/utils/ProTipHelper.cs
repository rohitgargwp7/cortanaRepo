using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Misc;
using System.Windows;

namespace windows_client.utils
{
    class ProTipHelper
    {
        private const string PROTIPS_DIRECTORY = "ProTips";
        
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();
        
        private static volatile ProTipHelper instance = null;
        private static ProTip _currentProTip = null;
        
        public event EventHandler<EventArgs> ShowProTip;

        public static ProTip CurrentProTip
        {
            get
            {
                return _currentProTip;
            }
            private set
            {
                _currentProTip = value;
            }
        }

        public static ProTipHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new ProTipHelper();

                            App.appSettings.TryGetValue(App.PRO_TIP, out _currentProTip);
                        }
                    }
                }
                return instance;
            }
        }

        public void AddProTip(string id, string header, string body, string imageUrl, string base64Image)
        {
            ProTip currentProTip = null;
            currentProTip = new ProTip(id, header, body, imageUrl,base64Image);

            if (currentProTip != null)
            {
                App.WriteToIsoStorageSettings(App.PRO_TIP, currentProTip);
                CurrentProTip = currentProTip;

                if (ShowProTip != null)
                    ShowProTip(null, null);
            }
        }

        public void ClearProTips()
        {
            if (CurrentProTip != null)
            {
                CurrentProTip = null;
                App.appSettings[App.PRO_TIP] = null;
                App.appSettings[App.PRO_TIP_COUNT] = 0;
            }
        }

        public void ClearOldProTips()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    var fileNames = store.GetFileNames(PROTIPS_DIRECTORY + "\\*");

                    var currentFile = PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(CurrentProTip.ImageUrl);

                    foreach (var fileName in fileNames)
                    {
                        if (store.FileExists(fileName) && fileName != currentFile)
                            store.DeleteFile(fileName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete ProTip Data on upgrade to 2.2.2.1, Exception : " + ex.StackTrace);
                }
            }
        }

        public void RemoveCurrentProTip()
        {
            if (CurrentProTip == null)
                return;

            if (!String.IsNullOrEmpty(CurrentProTip.ImageUrl))
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        var fileName = PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(CurrentProTip.ImageUrl);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete current ProTip Image File, Exception : " + ex.StackTrace);
                    }
                }
            }

            CurrentProTip = null;
        }
    }

    public class ProTip
    {
        public string _id;
        public string _header;
        public string _body;
        public string ImageUrl;
        public string Base64Image;

        ImageSource _tipImage;
        public ImageSource TipImage
        {
            get
            {
                if (_tipImage == null)
                {
                    if (Base64Image != null)
                        _tipImage = UI_Utils.Instance.createImageFromBytes(System.Convert.FromBase64String(Base64Image));
                    else
                        _tipImage = ProcesImageSource();

                    return _tipImage;
                }
                else
                    return _tipImage;
            }
        }

        private ImageSource ProcesImageSource()
        {
            ImageSource source = null;

            source = new BitmapImage();

            if (!String.IsNullOrEmpty(ImageUrl))
                ImageLoader.Load(source as BitmapImage, new Uri(ImageUrl), null, Utils.ConvertUrlToFileName(ImageUrl));

            return source;
        }

        public ProTip() { }

        public ProTip(string id, string header, string body, string imageUrl, string base64Image)
        {
            _id = id;
            _header = header;
            _body = body;
            ImageUrl = imageUrl;
            Base64Image = base64Image;
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                writer.WriteStringBytes(_id);
                
                if (_header == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_header);

                if (_body == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_body);


                if (ImageUrl == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(ImageUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ProTip :: Write : Unable To write, Exception : " + ex.StackTrace);
            }

        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                
                count = reader.ReadInt32();
                _header = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_header == "*@N@*")
                    _header = null;
                
                count = reader.ReadInt32();
                _body = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_body == "*@N@*")
                    _body = null;
                
                count = reader.ReadInt32();
                ImageUrl = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                
                if (ImageUrl == "*@N@*")
                    ImageUrl = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ContactInfo :: Read : Read, Exception : " + ex.StackTrace);
            }
        }
    }
}
