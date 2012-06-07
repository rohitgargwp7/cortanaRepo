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
        private int defaultBufferSize;

        public MessageStream(int size)
        {
            data = new byte[size];
            startIndex = 0;
            endIndex = 0;
            defaultBufferSize = 400;// if < 40 bytes are remaining to reach end of buffer, re-adjustment occurs
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
            return data[startIndex++];
        }

        public void writeByte(byte byteToWrite)
        {
            if (data.Length - endIndex + defaultBufferSize < data.Length)
            {
                reAdjustBuffer();
            }
            data[endIndex++] = byteToWrite;
        }

        public int Size()
        {
            if (endIndex == 0 && startIndex == -1)
                return 0;
            if(endIndex >= startIndex)
                return endIndex - startIndex;
            return data.Length - startIndex + endIndex;
        }

        private void reAdjustBuffer()
        {
            int j = 0;
            for (int i = startIndex; i < endIndex; i++, j++)
            {
                data[j] = data[i];
            }
            startIndex = 0;
            endIndex = j;
        }

        public void writeBytes(byte[] dataToWrite)
        {
            if (data.Length - endIndex < defaultBufferSize)
            {
                reAdjustBuffer();
            }
            Array.Copy(dataToWrite, 0, data, endIndex, dataToWrite.Length);
            endIndex += dataToWrite.Length;
        }

        public void writeBytes(byte[] dataToWrite, int start, int bytesToWrite)
        {
            if (data.Length - endIndex < bytesToWrite + defaultBufferSize)
            {
                reAdjustBuffer();
            }
            Array.Copy(dataToWrite, start, data, endIndex, bytesToWrite);
            endIndex += bytesToWrite;
        }
    }
}
