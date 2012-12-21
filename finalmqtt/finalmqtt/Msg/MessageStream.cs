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

        public bool containsCompleteMessage()
        {
            if (this.Size() == 0)
                return false;
            int temp = startIndex;
            startIndex = (startIndex + 1) % data.Length; //ignore byte containing flags
            int msgLength = readMsgLength();
            int sizeAfterReadingMessageLength = this.Size();
            startIndex = temp; //readjust start of buffer array to keep the data intact
            if (sizeAfterReadingMessageLength >= msgLength)
            {
                return true;
            }
            return false;
        }

        public byte[] ToArray()
        {
            byte[] messageData = new byte[this.Size()];
            if (startIndex < endIndex)
            {
                Buffer.BlockCopy(data, startIndex, messageData, 0, endIndex - startIndex);
            }
            else
            {
                Buffer.BlockCopy(data, startIndex, messageData, 0, data.Length - startIndex);
                Buffer.BlockCopy(data, 0, messageData, data.Length - startIndex, endIndex);
            }
            return messageData;
        }

        public MessageStream()
            : this(2048)
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
            if (endIndex >= startIndex)
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

            if ((startIndex < endIndex) || ((data.Length - startIndex) >= numberOfBytesToRead))
            {
                Buffer.BlockCopy(data, startIndex, dataToRead, 0, numberOfBytesToRead);
                startIndex += numberOfBytesToRead;
                startIndex %= data.Length;
            }
            else
            {
                int bytesBeforeRotation = data.Length - startIndex;
                Buffer.BlockCopy(data, startIndex, dataToRead, 0, bytesBeforeRotation);
                Buffer.BlockCopy(data, 0, dataToRead, bytesBeforeRotation, numberOfBytesToRead - bytesBeforeRotation);
                startIndex = numberOfBytesToRead - bytesBeforeRotation;
            }
            return dataToRead;
        }

        public int readMsgLength()
        {
            int msgLength = 0;
            int multiplier = 1;
            int digit;
            do
            {
                digit = data[startIndex];
                startIndex = (startIndex + 1) % data.Length;
                msgLength += (digit & 0x7f) * multiplier;
                multiplier *= 128;
            } while ((digit & 0x80) > 0);
            return msgLength;
        }

        public void insertMessageFlags(byte flags)
        {
            startIndex = (startIndex - 1) % data.Length;
            data[startIndex] = flags;
        }

        public void writeBytes(byte[] source, int start, int bytesToWrite)
        {
            if (bytesToWrite > data.Length - this.Size())
            {
                byte[] temp = new byte[data.Length * 2];
                int bytesBeforeRotation = data.Length - startIndex;

                Buffer.BlockCopy(data, startIndex, temp, 0, bytesBeforeRotation);
                Buffer.BlockCopy(data, 0, temp, bytesBeforeRotation, data.Length - bytesBeforeRotation);
                endIndex = this.Size();
                startIndex = 0;
                data = temp;
            }
            if (data.Length - endIndex >= bytesToWrite)
            {
                Buffer.BlockCopy(source, 0, data, endIndex, bytesToWrite);
                endIndex += bytesToWrite;
                endIndex %= data.Length;
            }
            else
            {
                int byteCountBeforeRotation = data.Length - endIndex;
                Buffer.BlockCopy(source, start, data, endIndex, byteCountBeforeRotation);
                Buffer.BlockCopy(source, start + byteCountBeforeRotation, data, 0, bytesToWrite - byteCountBeforeRotation);
                endIndex = bytesToWrite - byteCountBeforeRotation;
            }
        }
    }
}
