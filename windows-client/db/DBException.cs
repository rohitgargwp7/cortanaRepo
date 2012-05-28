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

namespace windows_client.db
{
    public class dbexception : Exception
    {
        public Exception parentexc;

        public dbexception(Exception e)
        {
            this.parentexc = e;
        }

        public string tostring()
        {
            return "dbexception " + this.parentexc.ToString();
        }
    }
}
