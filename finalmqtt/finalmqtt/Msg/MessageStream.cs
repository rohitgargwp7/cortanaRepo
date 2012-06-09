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
            Array.Copy(data, startIndex, messageData, 0, this.Size());
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
            if (endIndex == 0 && startIndex == -1)
                return 0;
            if(endIndex >= startIndex)
                return endIndex - startIndex;
            return data.Length - startIndex + endIndex + 1;
        }

        public void writeBytes(byte[] dataToWrite)
        {
            if (data.Length - endIndex >= dataToWrite.Length)
            {
                Array.Copy(dataToWrite, 0, data, endIndex, dataToWrite.Length);
                endIndex += dataToWrite.Length;
                endIndex %= data.Length;
            }
            else 
            {
                int byteCountBeforeRotation = data.Length - endIndex;
                Array.Copy(dataToWrite, 0, data, endIndex, byteCountBeforeRotation);
                Array.Copy(dataToWrite, byteCountBeforeRotation, data, 0, dataToWrite.Length - byteCountBeforeRotation);
                endIndex = dataToWrite.Length - byteCountBeforeRotation;
            }
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
