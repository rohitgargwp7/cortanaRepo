using System;
using System.IO;

namespace windows_client.FileTransfers
{
    public interface IFileInfo
    {
        int BytesTransfered { get; }
        double PercentageTransfer { get; }
        int TotalBytes { get; set; }
        string Id { get; set; }
        int CurrentHeaderPosition { get; set; }
        string ContentType { get; set; }
        string FileName { get; set; }
        string Msisdn { get; set; }
        FileTransferState FileState { get; set; }

        event EventHandler<FileTransferSatatusChangedEventArgs> StatusChanged;

        void Write(BinaryWriter writer);
        void Read(BinaryReader reader);
        void Save();
        void Delete();
        void Start(object obj);
    }
}
