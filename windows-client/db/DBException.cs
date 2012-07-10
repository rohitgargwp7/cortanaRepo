using System;

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
