using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace windows_client.FileTransfers
{
    public class BackgroundUploaderService
    {
        public BackgroundUploaderService()
        {
            _uploads = new List<BackgroundUploader>();
        }

        public void Add(BackgroundUploader bd)
        {
            Deployment.Current.Dispatcher.BeginInvoke(()=>
            {
                _uploads.Add(bd);
            });
        }

        public void Remove(BackgroundUploader bd)
        {
            Deployment.Current.Dispatcher.BeginInvoke(()=>
            {
                _uploads.Remove(bd);
            });
        }

        public Int32 Count
        {
            get
            {
                return _uploads.Count;
            }
        }

        private List<BackgroundUploader> _uploads
        {
            get;
            set;
        }
    }
}
