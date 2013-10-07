using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfers
{
    class FileTransferManager
    {

        private ConcurrentDictionary<long, FileTransferBase> fileTaskMap;

        private String HIKE_TEMP_DIR_NAME = "hikeTmp";

        private string HIKE_TEMP_DIR;

        // Constant variables
        private int THREAD_POOL_SIZE = 10;


        public static FileTransferManager _instance = null;


        private FileTransferManager()
        {
            fileTaskMap = new ConcurrentDictionary<long, FileTransferBase>();
            //HIKE_TEMP_DIR = context.getExternalFilesDir(HIKE_TEMP_DIR_NAME);
        }

        public static FileTransferManager getInstance()
        {
            if (_instance == null)
            {
                if (_instance == null)
                    _instance = new FileTransferManager();
            }
            return _instance;
        }


        //public void uploadFile(File destinationFile, String fileKey, long msgId, HikeFileType hikeFileType)
        //{
        //    UploadFileTask task = new UploadFileTask(context, destinationFile, fileKey, msgId, hikeFileType);
        //    pool.execute(task);
        //}

        public void removeTask(long msgId)
        {
            //fileTaskMap.remove(msgId);
        }

        /*
         * This function will close down the executor service and
         */
        public void shutDownAll()
        {
            // Todo : Handle properly
            //pool.shutdownNow();
        }

        public void cancelTask(long msgId)
        {
            //FileTransferBase fObj = fileTaskMap.get(msgId);
            //if(fObj != null)
            //    fObj.setState(FTState.CANCELLED);
        }

        public void pauseTask(long msgId)
        {
            //FileTransferBase fObj = fileTaskMap.get(msgId);
            //if(fObj != null)
            //    fObj.setState(FTState.PAUSED);
        }

        // this will be used when user deletes corresponding chat bubble
        public void deleteStateFile(long msgId, string mFile)
        {
            //String fName = msgId + "_" + mFile.getName() + ".bin";
            //File f = new File(HIKE_TEMP_DIR, fName);
            //if (f != null)
            //    f.delete();
        }

        // this will be used when user deletes account or unlink account
        public void deleteAllStateFile(long msgId, string mFile)
        {
            //String fName = msgId + "_" + mFile.getName() + ".bin";
            //File f = new File(HIKE_TEMP_DIR, fName);
            //f.delete();
        }

        public FileSavedState getFileState(long msgId, string mFile)
        {
            //Object obj = fileTaskMap.get(msgId);
            //if(obj != null)
            //{
            return new FileSavedState(FileTransfers.FileTransferBase.FTState.IN_PROGRESS, 0, 0);
            //}
            //else
            //    return getFileState(mFile);

        }

        /* here mFile is the file provided by the caller (original file)
         * null : represents unhandled error and should be handled accordingly
         */
        //public FileSavedState getFileState(File mFile)
        //{
        //    FileSavedState fss = null;
        //    if (mFile.exists())
        //    {
        //        fss = new FileSavedState(FTState.COMPLETED, 100, 100);
        //    }
        //    else
        //    {
        //        try
        //        {
        //            String fName = mFile.getName() + ".bin";
        //            File f = new File(HIKE_TEMP_DIR, fName);
        //            if(!f.exists())
        //                return null; 
        //            FileInputStream fileIn = new FileInputStream(f);
        //            ObjectInputStream in = new ObjectInputStream(fileIn);
        //            fss = (FileSavedState) in.readObject();
        //            in.close();
        //            fileIn.close();
        //        }
        //        catch (IOException i)
        //        {
        //            i.printStackTrace();
        //        }
        //        catch (ClassNotFoundException e)
        //        {
        //            // TODO Auto-generated catch block
        //            e.printStackTrace();
        //        }
        //    }
        //    return fss;
        //}

        //public File getHikeTempDir()
        //{
        //    return HIKE_TEMP_DIR;
        //}
    }
}
