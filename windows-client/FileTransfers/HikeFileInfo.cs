using System.IO;
namespace windows_client.FileTransfers
{
    public interface HikeFileInfo
    {
        int BytesTransfered { get; }
        double PercentageTransfer { get; }
        int TotalBytes { get; set; }
        string Id { get; set; }
        int CurrentHeaderPosition { get; set; }
        byte[] FileBytes { get; set; }
        string ContentType { get; set; }
        string FileName { get; set; }
        string Msisdn { get; set; }
        HikeFileState FileState { get; set; }

        void Write(BinaryWriter writer);
        void Read(BinaryReader reader);
    }

    public enum HikeFileState
    {
        NOT_STARTED,
        FAILED,
        STARTED,
        COMPLETED,
        CANCELED,
        PAUSED,
        MANUAL_PAUSED
    }
}
