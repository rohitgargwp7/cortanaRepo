using CommonLibrary.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Utils
{
    public static class Utility
    {
        /// <summary>
        /// Read and calculate md5 by parts from file
        /// </summary>
        /// <param name="filePath">file path for file of which md5 needs to be calculated</param>
        /// <returns>md5 string</returns>
        public static string GetMD5Hash(string filePath)
        {
            byte[] buffer;
            byte[] oldBuffer;
            int bytesRead;
            int oldBytesRead;
            long size;
            long totalBytesRead = 0;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                    {
                        using (MD5 hashAlgorithm = MD5.Create())
                        {
                            size = fileStream.Length;
                            buffer = new byte[4096];
                            bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                            totalBytesRead += bytesRead;

                            do
                            {
                                oldBytesRead = bytesRead;
                                oldBuffer = buffer;

                                buffer = new byte[4096];
                                bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                                totalBytesRead += bytesRead;

                                if (bytesRead == 0)
                                    hashAlgorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                                else
                                    hashAlgorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);

                            } while (bytesRead != 0);

                            StringBuilder sb = new StringBuilder();

                            foreach (byte b in hashAlgorithm.Hash)
                                sb.Append(b.ToString("x2"));

                            return sb.ToString();
                        }
                    }
                }
                else
                    return string.Empty;
            }
        }
    }
}
