using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer
{
    public class FileTransferSatatusChangedEventArgs : EventArgs
    {
        public FileTransferSatatusChangedEventArgs(FileInfoBase fileInfo, bool isStateChanged)
        {
            FileInfo = fileInfo;
            IsStateChanged = isStateChanged;
        }

        public FileInfoBase FileInfo { get; private set; }
        public bool IsStateChanged { get; private set; }
    }
}
