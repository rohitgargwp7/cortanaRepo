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
        private const string CURRENT_PROTIP_TN = "CurrentProTipThumbnail";

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
            set
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
                            string id = String.Empty;
                            App.appSettings.TryGetValue(App.PRO_TIP, out id);

                            if (!String.IsNullOrEmpty(id))
                                ReadFromFile(id);
                        }
                    }
                }
                return instance;
            }
        }

        public void AddProTip(string id, string header, string body, string imageUrl, string base64Image)
        {
            CurrentProTip = new ProTip(id, header, body, imageUrl, base64Image);

            App.WriteToIsoStorageSettings(App.PRO_TIP, id);

            WriteProTipToFile();

            if (ShowProTip != null)
                ShowProTip(null, null);
        }

        public void WriteProTipToFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + CurrentProTip.Id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(PROTIPS_DIRECTORY))
                            store.CreateDirectory(PROTIPS_DIRECTORY);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                CurrentProTip.Write(writer);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Write ProTip To File, Exception : " + ex.StackTrace);
                }
            }
        }

        static void ReadFromFile(String id)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(PROTIPS_DIRECTORY))
                            return;

                        if (!store.FileExists(fileName))
                            return;
                        
                        CurrentProTip = new ProTip();

                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader reader = new BinaryReader(file))
                            {
                                CurrentProTip.Read(reader);
                                reader.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Read ProTip From File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void ClearProTips()
        {
            if (CurrentProTip != null)
            {
                RemoveCurrentProTip();

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
                lock (readWriteLock)
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            string fileName = PROTIPS_DIRECTORY + "\\" + CurrentProTip.Id;

                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);

                            fileName = PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(CurrentProTip.ImageUrl);

                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete current ProTip File, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }

            CurrentProTip = null;
        }

        public class ProTip
        {
            public string Id;
            public string Header;
            public string Body;
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
                Id = id;
                Header = header;
                Body = body;
                ImageUrl = imageUrl;
                Base64Image = base64Image;
            }

            public void Write(BinaryWriter writer)
            {
                try
                {
                    writer.WriteStringBytes(Id);

                    if (Header == null)
                        writer.WriteStringBytes("*@N@*");
                    else
                        writer.WriteStringBytes(Header);

                    if (Body == null)
                        writer.WriteStringBytes("*@N@*");
                    else
                        writer.WriteStringBytes(Body);

                    if (ImageUrl == null)
                        writer.WriteStringBytes("*@N@*");
                    else
                        writer.WriteStringBytes(ImageUrl);

                    if (Base64Image == null)
                        writer.WriteStringBytes("*@N@*");
                    else
                        writer.WriteStringBytes(Base64Image);
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
                    Id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                    count = reader.ReadInt32();
                    Header = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (Header == "*@N@*")
                        Header = null;

                    count = reader.ReadInt32();
                    Body = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (Body == "*@N@*")
                        Body = null;

                    count = reader.ReadInt32();
                    ImageUrl = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (ImageUrl == "*@N@*")
                        ImageUrl = null;

                    count = reader.ReadInt32();
                    Base64Image = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (Base64Image == "*@N@*")
                        Base64Image = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip :: Read : Read, Exception : " + ex.StackTrace);
                }
            }
        }
    }
}
