using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfers
{
    class UploadFileTask : FileTransferBase
    {
        private int chunkSize;

        private string mUrl;

        private String X_SESSION_ID;

        private String uid;

        private String fileType;

        private static String BOUNDARY = "----------V2ymHFg03ehbqgZCaKO6jy";

        protected UploadFileTask(string destinationFile, String fileKey, long msgId)
            : base(destinationFile, fileKey, msgId)
        {
        }

        public void run()
        {
            try
            {
                //FileSavedState fst = FileTransferManager.getInstance(context).getFileState(mFile);
                //if (fst == null || fst.getFTState().equals(FTState.CANCELLED)) // represents this file is either not started or unrecovered error has happened
                //{
                //    setFileTotalSize((int)mFile.length());
                //}
                //else if (fst.getFTState().equals(FTState.PAUSED) || fst.getFTState().equals(FTState.ERROR))
                //{

                //}
                //mUrl = new URL(AccountUtils.partialfileTransferBaseUrl);
                //FileInputStream is = new FileInputStream(mFile);
                long length = mFile.Length;
                int numberOfChunks = ((int)length % chunkSize == 0) ? ((int)length / chunkSize) : (int)length / chunkSize + 1;
                for (int i = 0; i < numberOfChunks; i++)
                {
                    bool last = i == numberOfChunks - 1;
                    byte[] fileBytes = getFileBytesBeginning((int)length, last);
                    int start = i * chunkSize;
                    int end = (i == numberOfChunks - 1) ? (int)length - 1 : start + chunkSize - 1;
                    String contentRange = "bytes " + start + "-" + end + "/" + length;
                    byte[] response = send(contentRange, fileBytes);
                    //String responseString = new String(response);
                    //System.out.println(responseString);
                }
                //is.close();
            }
            //catch (MalformedURLException e)
            //{
            //    // TODO Auto-generated catch block
            //    e.printStackTrace();
            //}
            //catch (FileNotFoundException e)
            //{
            //    // TODO Auto-generated catch block
            //    e.printStackTrace();
            //}
            catch (Exception e)
            {
                // TODO Auto-generated catch block
                //e.printStackTrace();
            }
        }

        private byte[] getFileBytesBeginning(int length, bool last)
        {
            byte[] arr = null;
            //if (!last)
            //{
            //    arr = new byte[chunkSize];
            //}
            //else
            //{
            //    arr = new byte[length % chunkSize];
            //}

            //is.read(arr);
            return arr;
        }

        String getBoundaryMessage(String contentRange)
        {
            StringBuilder res = new StringBuilder("--").Append(BOUNDARY).Append("\r\n");
            res.Append("Content-Disposition: form-data; name=\"");
            res.Append("Cookie").Append("\"\r\n").Append("\r\n");
            res.Append(uid).Append("\r\n").Append("--").Append(BOUNDARY).Append("\r\n");
            res.Append("Content-Disposition: form-data; name=\"");
            res.Append("X-CONTENT-RANGE").Append("\"\r\n").Append("\r\n");
            res.Append(contentRange).Append("\r\n").Append("--").Append(BOUNDARY).Append("\r\n");
            res.Append("Content-Disposition: form-data; name=\"");
            res.Append("X-SESSION-ID").Append("\"\r\n").Append("\r\n");
            res.Append(X_SESSION_ID).Append("\r\n").Append("--").Append(BOUNDARY).Append("\r\n");
            res.Append("Content-Disposition: form-data; name=\"").Append("file").Append("\"; filename=\"").Append("#####").Append("\"\r\n").Append("Content-Type: ")
            .Append(fileType).Append("\r\n\r\n");
            return res.ToString();
        }

        private byte[] send(String contentRange, byte[] fileBytes)
        {
            //HttpURLConnection hc = null;
            //InputStream is = null;
            //ByteArrayOutputStream bos = new ByteArrayOutputStream();

            byte[] res = null;

            //try
            //{
            //    hc = (HttpURLConnection)mUrl.openConnection();
            //    /*
            //     * Setting request headers
            //     * 
            //     */

            //    hc.addRequestProperty("X-SESSION-ID", "put here"); 
            //    hc.addRequestProperty("X-CONTENT-RANGE", contentRange);
            //    hc.addRequestProperty("Cookie", "TODO");
            //    hc.setRequestProperty("Content-Type", "multipart/form-data; boundary=" + BOUNDARY);
            //    hc.setRequestMethod("POST");
            //    hc.setDoInput(true);
            //    hc.setDoOutput(true);

            //    byte [] postBytes = getPostBytes(contentRange,fileBytes);

            //    bos.close();
            //    OutputStream dout = hc.getOutputStream();

            //    dout.write(postBytes);

            //    dout.close();

            //    int ch;

            //    is = hc.getInputStream();

            //    while ((ch = is.read()) != -1)
            //    {
            //        bos.write(ch);
            //    }
            //    res = bos.toByteArray();
            //}
            //catch (Exception e)
            //{
            //    e.printStackTrace();
            //}
            //finally
            //{
            //    try
            //    {
            //        if (bos != null)
            //            bos.close();

            //        if (is != null)
            //            is.close();

            //        if (hc != null)
            //            hc.disconnect();
            //    }
            //    catch (Exception e2)
            //    {
            //        e2.printStackTrace();
            //    }
            //}
            return res;
        }

        private byte[] getPostBytes(String contentRange, byte[] fileBytes)
        {
            //ByteArrayOutputStream bos = new ByteArrayOutputStream();
            byte[] postBytes = null;
            //try
            //{
            //    bos.write(getBoundaryMessage(contentRange).getBytes());
            //    bos.write(fileBytes);
            //    bos.write(("\r\n--" + BOUNDARY + "--\r\n").getBytes());
            //    postBytes = bos.toByteArray();
            //}
            //catch (IOException e)
            //{
            //    // TODO Auto-generated catch block
            //    e.printStackTrace();
            //}
            //finally
            //{
            //    if(bos != null)
            //        try
            //        {
            //            bos.close();
            //        }
            //        catch (IOException e)
            //        {
            //            // TODO Auto-generated catch block
            //            e.printStackTrace();
            //        }
            //}

            return postBytes;
        }
    }
}
