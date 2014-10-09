using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using windows_client.Misc;
using System.Threading.Tasks;
using System.Diagnostics;

namespace windows_client.utils
{
    public static class ExceptionLoggingHelper
    {
        public static readonly string EXCEPTION_FOLDER = "exceptions";
        /// <summary>
        /// {0}-DateTime
        /// {1}-Exception Message
        /// {2}-Exception Stacktrace
        /// {3}-Current App memory in use
        /// </summary>
        public static readonly string EXCEPTION_MESSAGE = "{0}\nMessage: {1}\nStacktrace:\n{2}\nMemory in use: {3:0.00}MB";


        public static void WriteExceptionToFile(string message, string stacktrace)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(EXCEPTION_FOLDER))
                {
                    store.CreateDirectory(EXCEPTION_FOLDER);
                }
                string fileName = EXCEPTION_FOLDER + "\\" + TimeUtils.getCurrentTimeTicks();
                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (BinaryWriter writer = new BinaryWriter(file))
                    {
                        writer.WriteString(string.Format(EXCEPTION_MESSAGE, DateTime.Now, message, stacktrace, (Microsoft.Phone.Info.DeviceStatus.ApplicationCurrentMemoryUsage / (1024 * 1024)).ToString()));
                        writer.Flush();
                        writer.Close();
                    }
                }
            }
        }

        public static string[] GetAllExceptions()
        {
            string[] arrExceptions = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.DirectoryExists(EXCEPTION_FOLDER))
                {
                    string[] fileNames = store.GetFileNames(EXCEPTION_FOLDER + "\\*");
                    if (fileNames != null && fileNames.Length > 0)
                    {
                        arrExceptions = new string[fileNames.Length];

                        for (int i = 0; i < arrExceptions.Length; i++)
                        {
                            string fileName = EXCEPTION_FOLDER + "\\" + fileNames[i];
                            try
                            {
                                using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                                {
                                    using (var reader = new BinaryReader(file))
                                    {
                                        arrExceptions[i] = reader.ReadString();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                arrExceptions[i] = string.Empty;
                                Debug.WriteLine("ExceptionLoggingHelper::GetAllExceptions:File:{0},Exception:{1},Stacktrace:{2}", fileName, ex.Message, ex.StackTrace);
                            }
                        }
                    }
                }

            }
            return arrExceptions;
        }

        public static void DeleteAllExceptions()
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.DirectoryExists(EXCEPTION_FOLDER))
                    {
                        string[] files = store.GetFileNames(EXCEPTION_FOLDER + "\\*");
                        if (files != null)
                            foreach (string exceptionFile in files)
                            {
                                string fileName = EXCEPTION_FOLDER + "\\" + exceptionFile;
                                if (store.FileExists(fileName))
                                    store.DeleteFile(fileName);
                            }
                        store.DeleteDirectory(EXCEPTION_FOLDER);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
