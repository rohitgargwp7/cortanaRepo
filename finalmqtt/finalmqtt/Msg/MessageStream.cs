using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.IO;

namespace finalmqtt.Msg
{
    public class MessageStream
    {
        private byte[] data;
        private int startIndex;
        private int endIndex;

        public MessageStream(int size)
        {
            data = new byte[size];
            startIndex = 0;
            endIndex = 0;
        }

        public byte[] ToArray()
        { 
            byte[] messageData = new byte[this.Size()];
            if (startIndex < endIndex)
            {
                Array.Copy(data, startIndex, messageData, 0, endIndex - startIndex);
            }
            else
            {
                Array.Copy(data, startIndex, messageData, 0, data.Length - startIndex);
                Array.Copy(data, 0, messageData, data.Length - startIndex, endIndex);
            }
            return messageData;
        }

        public MessageStream():this(2048)
        {
        }

        public byte readByte()
        {
            if (startIndex == endIndex)
                throw new IndexOutOfRangeException("Nothing to read");
            int temp = startIndex;
            startIndex = (startIndex + 1) % data.Length;
            return data[temp];
        }

        public void writeByte(byte byteToWrite)
        {
            data[endIndex] = byteToWrite;
            endIndex = (endIndex + 1) % data.Length;
        }

        public int Size()
        {
            if(endIndex >= startIndex)
                return endIndex - startIndex;
            return data.Length - startIndex + endIndex;
        }

        public void writeBytes(byte[] dataToWrite)
        {
            writeBytes(dataToWrite, 0, dataToWrite.Length);
        }

        public byte[] readBytes(int numberOfBytesToRead)
        {
            if (numberOfBytesToRead <= 0)
            {
                throw new ArgumentException("Number of bytes to read should be greater than 0");
            }
            if (this.Size() < numberOfBytesToRead)
            {
                throw new IndexOutOfRangeException("requested for " + numberOfBytesToRead + "bytes, where only " + this.Size() + " exist in buffer");
            }
            byte[] dataToRead = new byte[numberOfBytesToRead];

            if (startIndex < endIndex)
            {
                Array.Copy(data, startIndex, dataToRead, 0, numberOfBytesToRead);
                startIndex += numberOfBytesToRead;
            }
            else
            {
                int bytesBeforeRotation = data.Length - startIndex;
                Array.Copy(data, startIndex, dataToRead, 0, bytesBeforeRotation);
                Array.Copy(data, 0, dataToRead, bytesBeforeRotation, numberOfBytesToRead - bytesBeforeRotation);
                startIndex = numberOfBytesToRead - bytesBeforeRotation;
            }
            return dataToRead;
        }

        public void writeBytes(byte[] source, int start, int bytesToWrite)
        {
            if (data.Length - endIndex >= bytesToWrite)
            {
                Array.Copy(source, 0, data, endIndex, bytesToWrite);
                endIndex += bytesToWrite;
                endIndex %= data.Length;
            }
            else
            {
                int byteCountBeforeRotation = data.Length - endIndex;
                Array.Copy(source, start, data, endIndex, byteCountBeforeRotation);
                Array.Copy(source, start + byteCountBeforeRotation, data, 0, bytesToWrite - byteCountBeforeRotation);
                endIndex = bytesToWrite - byteCountBeforeRotation;
            }
        }
    }
}
