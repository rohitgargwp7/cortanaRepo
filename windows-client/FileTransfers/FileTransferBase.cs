using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace FileTransfers
{
    public abstract class FileTransferBase
    {
        public enum FTState
        {
            NOT_STARTED, IN_PROGRESS, // DOWNLOADING OR UPLOADING
            PAUSED, CANCELLED, COMPLETED, ERROR
        }


        protected static int BUFFER_SIZE = 4096;

        // this will be used for filename in download and upload both
        protected string mFile;

        protected string stateFile; // this represents state file in which file state will be saved

        protected volatile FTState _state;

        protected long msgId;

        protected String fileKey; // this is used for download from server

       // protected HikeFileType hikeFileType;

        protected int _totalSize = 0;

        protected int _bytesTransferred = 0;

        protected FileTransferBase(string destinationFile, String fileKey, long msgId)
        {
            this.mFile = destinationFile;
            this.fileKey = fileKey;
            this.msgId = msgId;
        }

        protected void setFileTotalSize(int ts)
        {
            _totalSize = ts;
        }

        // this will be used for both upload and download
        protected void setBytesTransferred(int value)
        {
            _bytesTransferred += value;
        }

        protected void saveFileState()
	{
		FileSavedState fss = new FileSavedState(_state, _totalSize, _bytesTransferred);
		try
		{
            //FileOutputStream fileOut = new FileOutputStream(stateFile);
            //ObjectOutputStream out = new ObjectOutputStream(fileOut);
            //out.writeObject(fss);
            //out.close();
            //fileOut.close();
		}
		catch (IOException i)
		{
            //i.printStackTrace();
		}
	}

        protected void deleteStateFile()
        {
            //if (stateFile != null && stateFile.exists())
            //    stateFile.delete();
        }

        protected void setState(FTState mState)
        {
            // if state is completed we will not change it '
            //if (!mState.equals(FTState.COMPLETED))
            //    _state = mState;
        }
    }
}
