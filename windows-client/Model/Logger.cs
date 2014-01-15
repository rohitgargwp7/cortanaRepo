using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace Logging
{
    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// </summary>
    public class LogWriter
    {
        private static LogWriter instance;
        private static Queue<Log> logQueue;
        private static string logDir = "Logs";
        private static string logFile = "logfile.txt";
        private static int maxLogAge = 1;
        private static int queueSize = 10;
        private static DateTime LastFlushed = DateTime.Now;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private LogWriter() { }

        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static LogWriter Instance
        {
            get
            {
                // If the instance is null then create one and init the Queue
                if (instance == null)
                {
                    instance = new LogWriter();
                    logQueue = new Queue<Log>();
                }
                return instance;
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public async void WriteToLog(string message)
        {
            await Task.Delay(1);

            // Lock the queue while writing to prevent contention for the log file
            lock (logQueue)
            {
                // Create the entry and push to the Queue
                Log logEntry = new Log(message);
                logQueue.Enqueue(logEntry);

                // If we have reached the Queue Size then flush the Queue
                if (logQueue.Count >= queueSize || DoPeriodicFlush())
                {
                    FlushLog();
                }
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= maxLogAge)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private void FlushLog()
        {
            string logPath = logDir + "\\" + logFile;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(logDir))
                {
                    store.CreateDirectory(logDir);
                }
                using (var file = store.OpenFile(logPath, FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    using (StreamWriter writer = new StreamWriter(file))
                    {
                        while (logQueue.Count > 0)
                        {
                            Log entry = logQueue.Dequeue();
                            writer.Write(string.Format(" {0}:\t{1} ", entry.LogDate, entry.Message));
                            writer.Write(Environment.NewLine);
                        }
                    }
                }
            }
        }

        public string ReadFile()
        {
            string filedata = string.Empty;

            lock (logQueue)
            {
                string logPath = logDir + "\\" + logFile;
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(logPath))
                    {
                        using (var file = store.OpenFile(logPath, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = new StreamReader(file))
                            {
                                filedata = reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            return filedata;
        }
        public void ClearLogs()
        {
            lock (logQueue)
            {
                string logPath = logDir + "\\" + logFile;
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(logDir))
                    {
                        store.CreateDirectory(logDir);
                    }
                    store.DeleteFile(logPath);
                }
            }
        }
        ~LogWriter()
        {
            FlushLog();
        }

    }

    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    public class Log
    {
        public string Message { get; set; }
        public string LogDate { get; set; }

        public Log(string message)
        {
            Message = message;
            LogDate = DateTime.Now.ToString("MM-dd, hh:mm:ss");
        }
    }
}