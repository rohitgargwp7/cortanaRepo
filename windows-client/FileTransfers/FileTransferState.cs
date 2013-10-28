namespace windows_client.FileTransfers
{
    public enum FileTransferState
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
