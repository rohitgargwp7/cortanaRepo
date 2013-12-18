using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace windows_client.Model
{
    public class HikeToolTip
    {
        public string Tip { get; set; }
        public bool IsTop { get; set; }
        public Thickness TipMargin { get; set; }
        public Thickness FullTipMargin { get; set; }
        public bool IsShown { get; set; }
        public bool IsCurrentlyShown { get; set; }
        public HorizontalAlignment HAlingment { get; set; }
        public String Width { get; set; }
        public bool IsAnimationEnabled { get; set; }
        public BitmapImage TipImage { get; set; }

        public void TriggerUIUpdateOnDismissed()
        {
            if (TipDismissed != null)
                TipDismissed(null, null);
        }

        public event EventHandler<EventArgs> TipDismissed;
    }
}
