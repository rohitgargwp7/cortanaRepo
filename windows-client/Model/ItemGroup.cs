using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using windows_client.utils;

namespace windows_client.Model
{
    public class ItemGroup<T> : List<T>
    {
        public ItemGroup(string name)
        {
            this.Title = name;
        }

        public string Title
        {
            get;
            set;
        }

        public bool IsNonEmpty
        {
            get
            {
                return this.Count > 0;
            }
        }

        public SolidColorBrush ForegroundBrush
        {
            get
            {
                if (IsNonEmpty)
                    return UI_Utils.Instance.White;
                else
                    return UI_Utils.Instance.Gray;
            }
        }

        public SolidColorBrush BackgroundBrush
        {
            get
            {
                if (IsNonEmpty)
                    return UI_Utils.Instance.HikeBlue;
                else
                    return UI_Utils.Instance.DarkGray;
            }
        }
    }
}
