﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
        public SolidColorBrush Foreground { get; set; }
        public SolidColorBrush Background { get; set; }

        public void TriggerUIUpdateOnDismissed()
        {
            if (TipDismissed != null)
                TipDismissed(null, null);
        }

        public event EventHandler<EventArgs> TipDismissed;
    }
}
