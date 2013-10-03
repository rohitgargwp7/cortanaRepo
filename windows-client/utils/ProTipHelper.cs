﻿using System;
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
        readonly Int32 MAX_QUEUE_SIZE = 1000;
        private const string PROTIPS_DIRECTORY = "ProTips";
        private const string proTipsListFileName = "proTipList";
        private const string deletedProTipsListFileName = "delProTipList";

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        private static volatile ProTipHelper instance = null;
        public DispatcherTimer proTipTimer;
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

                            if (_currentProTip == null)
                                instance.StartTimer();
                        }
                    }
                }
                return instance;
            }
        }

        public void AddProTip(string id, string header, string body, string imageUrl)
        {
            if (_deletedTips == null)
                ReadDeletedTipsFromFile();

            if (_deletedTips != null && _deletedTips.Count > 0 && _deletedTips.Contains(id))
                return;

            if (_proTipsQueue == null)
                ReadProTipIdsFromFile();

            if (_proTipsQueue != null && (_proTipsQueue.Contains(id) || (CurrentProTip != null && CurrentProTip._id == id)))
                return;
            else if (_proTipsQueue == null)
                _proTipsQueue = new Queue<string>();

            if (_proTipsQueue.Count == MAX_QUEUE_SIZE)
                RemoveProTip(GetProTipFromFile(_proTipsQueue.Dequeue()));

            WriteProTipToFile(id, header, body, imageUrl);

            ProTip currentProTip = null;
            App.appSettings.TryGetValue<ProTip>(App.PRO_TIP, out currentProTip);

            if (currentProTip == null && (proTipTimer == null || !proTipTimer.IsEnabled))
            {
                if (_proTipsQueue.Count == 0)
                    currentProTip = new ProTip(id, header, body, imageUrl);
                else
                    currentProTip = GetProTipFromFile(_proTipsQueue.Dequeue());

                if (currentProTip != null)
                {
                    App.WriteToIsoStorageSettings(App.PRO_TIP, currentProTip);
                    CurrentProTip = currentProTip;

                    if (ShowProTip != null)
                        ShowProTip(null, null);
                }
            }
            else
                _proTipsQueue.Enqueue(id);

            WriteProTipIdsToFile(); // new protip added, persist it before-hand in case of crash
        }

        void getNextProTip()
        {
            if (_proTipsQueue == null)
                ReadProTipIdsFromFile();

            if (_proTipsQueue != null && _proTipsQueue.Count > 0)
            {
                CurrentProTip = GetProTipFromFile(_proTipsQueue.Dequeue());

                WriteProTipIdsToFile(); // new protip fetched from file. write changes

                if (CurrentProTip != null)
                {
                    App.WriteToIsoStorageSettings(App.PRO_TIP, CurrentProTip);

                    if (ShowProTip != null)
                        ShowProTip(null, null);

                    return;
                }
            }

            if (CurrentProTip == null && _proTipsQueue != null && _proTipsQueue.Count > 0)
                getNextProTip();
        }

        public void ChangeTimerTime(Int64 time)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action<Int64>(delegate(Int64 newTime)
            {
                if (proTipTimer != null)
                    proTipTimer.Interval = TimeSpan.FromSeconds(newTime); //time might have changed, hence reinitializing timer
            }), time);
        }

        public void ClearProTips()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        var fileNames = store.GetFileNames(PROTIPS_DIRECTORY + "\\*");
                        foreach (var name in fileNames)
                        {
                            string fName = PROTIPS_DIRECTORY + "\\" + name;

                            if (store.FileExists(fName))
                                store.DeleteFile(fName);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ProTip Helper :: Delete ProTip ids, Exception : " + ex.StackTrace);
                    }
                }
            }

            if (_proTipsQueue != null)
                _proTipsQueue.Clear();

            if (_deletedTips != null)
                _deletedTips.Clear();

            if (CurrentProTip != null)
            {
                CurrentProTip = null;
                App.appSettings[App.PRO_TIP] = null;
                App.appSettings[App.PRO_TIP_COUNT] = 0;
                App.WriteToIsoStorageSettings(App.PRO_TIP_DISMISS_TIME, HikeConstants.DEFAULT_PRO_TIP_TIME);
            }
        }

        public void RemoveCurrentProTip()
        {
            if (CurrentProTip == null)
                return;

            if (_deletedTips == null)
                ReadDeletedTipsFromFile();

            if (_deletedTips != null)
            {
                _deletedTips.Add(CurrentProTip._id);
                WriteDeletedProTipIdsToFile();
            }

            if (!String.IsNullOrEmpty(CurrentProTip.ImageUrl))
            {
                lock (readWriteLock)
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            string fileName = PROTIPS_DIRECTORY + "\\" + CurrentProTip._id;

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

        public void RemoveProTip(ProTip tip)
        {
            if (tip == null)
                return;

            if (_deletedTips == null)
            {
                ReadDeletedTipsFromFile();
                _deletedTips.Add(tip._id);
            }

            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        string fileName = PROTIPS_DIRECTORY + "\\" + tip._id;

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        fileName = PROTIPS_DIRECTORY + "\\" + Utils.ConvertUrlToFileName(tip.ImageUrl);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ProTip Helper :: delete ProTip id, Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        void ReadDeletedTipsFromFile()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    try
                    {
                        string fileName = PROTIPS_DIRECTORY + "\\" + deletedProTipsListFileName;

                        if (_deletedTips == null)
                            _deletedTips = new List<string>();

                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (BinaryReader reader = new BinaryReader(file))
                                {
                                    int count = reader.ReadInt32();

                                    for (int i = 0; i < count; i++)
                                        _deletedTips.Add(reader.ReadString());

                                    reader.Close();
                                }

                                file.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ProTip Helper :: Get ProTip ids From File, Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        void WriteDeletedProTipIdsToFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + deletedProTipsListFileName;
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
                                writer.Write(_deletedTips.Count);

                                foreach (var id in _deletedTips)
                                    writer.Write(id);

                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Write ProTip Ids To File, Exception : " + ex.StackTrace);
                }
            }
        }

        void WriteProTipIdsToFile()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + proTipsListFileName;
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
                                writer.Write(_proTipsQueue.Count);

                                foreach (var id in _proTipsQueue)
                                    writer.Write(id);

                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Write ProTip Ids To File, Exception : " + ex.StackTrace);
                }
            }
        }

        void ReadProTipIdsFromFile()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    try
                    {
                        string fileName = PROTIPS_DIRECTORY + "\\" + proTipsListFileName;
                        if (store.FileExists(fileName))
                        {
                            if (_proTipsQueue == null)
                                _proTipsQueue = new Queue<string>();

                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (BinaryReader reader = new BinaryReader(file))
                                {
                                    int count = reader.ReadInt32();

                                    for (int i = 0; i < count; i++)
                                        _proTipsQueue.Enqueue(reader.ReadString());

                                    reader.Close();
                                }

                                file.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ProTip Helper :: Get ProTip ids From File, Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        void WriteProTipToFile(string id, string header, string body, string imageUrl)
        {
            lock (readWriteLock)
            {
                ProTip proTip = new ProTip(id, header, body, imageUrl);

                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(PROTIPS_DIRECTORY))
                            store.CreateDirectory(PROTIPS_DIRECTORY);

                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                proTip.Write(writer);
                                writer.Flush();
                                writer.Close();
                            }

                            file.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: WriteProTip To File, Exception : " + ex.StackTrace);
                }
            }
        }

        ProTip GetProTipFromFile(string id)
        {
            ProTip proTip = null;

            lock (readWriteLock)
            {
                try
                {
                    string fileName = PROTIPS_DIRECTORY + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    proTip = new ProTip();

                                    var count = reader.ReadInt32();
                                    proTip._id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                                    count = reader.ReadInt32();
                                    proTip._header = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    if (proTip._header == "*@N@*")
                                        proTip._header = null;

                                    count = reader.ReadInt32();
                                    proTip._body = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    if (proTip._body == "*@N@*")
                                        proTip._body = null;

                                    count = reader.ReadInt32();
                                    proTip.ImageUrl = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                                    if (proTip.ImageUrl == "*@N@*")
                                        proTip.ImageUrl = null;
                                }

                                file.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    proTip = null;
                    System.Diagnostics.Debug.WriteLine("ProTip Helper :: Get ProTip From File, Exception : " + ex.StackTrace);
                }
            }

            return proTip;
        }

        public void StartTimer()
        {
            DateTime dt;
            if (!App.appSettings.TryGetValue(App.PRO_TIP_LAST_DISMISS_TIME, out dt))
            {
                getNextProTip();
                return;
            }

            TimeSpan ts = DateTime.Now.Subtract(dt);

            Int64 time = 0;
            App.appSettings.TryGetValue(App.PRO_TIP_DISMISS_TIME, out time);

            if (time > 0 && time > ts.TotalSeconds)
            {
                if (proTipTimer == null)
                {
                    proTipTimer = new DispatcherTimer();
                    proTipTimer.Tick -= proTipTimer_Tick;
                    proTipTimer.Tick += proTipTimer_Tick;
                }

                proTipTimer.Interval = TimeSpan.FromSeconds(time - ts.TotalSeconds); //time might have changed, hence reinitializing timer

                proTipTimer.Start();
            }
            else
                getNextProTip();
        }

        void proTipTimer_Tick(object sender, EventArgs e)
        {
            proTipTimer.Stop();
            getNextProTip();
        }

        Queue<string> _proTipsQueue;
        List<string> _deletedTips;
    }

    public class ProTip
    {
        public string _id;
        public string _header;
        public string _body;
        public string ImageUrl;

        ImageSource _tipImage;
        public ImageSource TipImage
        {
            get
            {
                if (_tipImage == null)
                {
                    _tipImage = ProcesImageSource();
                    return _tipImage;
                }
                else
                    return _tipImage;
            }
        }

        private ImageSource ProcesImageSource()
        {
            ImageSource source = new BitmapImage();

            if (!String.IsNullOrEmpty(ImageUrl))
                ImageLoader.Load(source as BitmapImage, new Uri(ImageUrl), null, Utils.ConvertUrlToFileName(ImageUrl));

            return source;
        }

        public ProTip() { }

        public ProTip(string id, string header, string body, string imageUrl)
        {
            _id = id;
            _header = header;
            _body = body;
            ImageUrl = imageUrl;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    _tipImage = ProcesImageSource();
                });
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
