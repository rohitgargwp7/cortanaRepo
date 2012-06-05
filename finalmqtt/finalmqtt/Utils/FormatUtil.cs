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

namespace mqtttest.Utils
{
    public class FormatUtil
    {
        public static byte[] toMQttString(String s)
        {
            if (s == null)
                return new byte[0];
            byte[] s2 = (new System.Text.UTF8Encoding()).GetBytes(s);
            int length = s2.Length;
            List<byte> mqttString = new List<byte>();

            mqttString.Add((byte)(length >> 8));
            mqttString.Add((byte)(length & 0xFF));
            
            mqttString.AddRange(s2);
            return mqttString.ToArray();
        }

        public static byte[] toMQttString(int val)
        {
            byte[] data = new byte[2];
            data[0] = (byte)(val >> 8);
            data[1] = (byte)(val & 0xFF);
            return data;
        }

    }
}
