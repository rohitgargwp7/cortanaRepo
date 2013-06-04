using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace windows_client.Model
{
    public class ToolTip
    {
        public string Tip { get; set; }
        public bool IsTop { get; set; }
        public Thickness TipMargin { get; set; }
        public Thickness FullTipMargin { get; set; }
        public bool IsShown { get; set; }
        public bool IsCurrentlyShown { get; set; }
    }
}
