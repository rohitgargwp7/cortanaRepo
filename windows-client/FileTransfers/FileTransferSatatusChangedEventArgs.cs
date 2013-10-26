using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.FileTransfers
{
    public class FileTransferSatatusChangedEventArgs : EventArgs
    {
        public FileTransferSatatusChangedEventArgs(IFileInfo fileInfo, bool isStateChanged)
        {
            FileInfo = fileInfo;
            IsStateChanged = isStateChanged;
        }

        public IFileInfo FileInfo { get; private set; }
        public bool IsStateChanged { get; private set; }
    }
}
