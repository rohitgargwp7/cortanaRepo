using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace windows_client.Controls
{
    public class CustomListBox : ListBox
    {
        public void ScrollToBottom()
        {
            var scrollviewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            scrollviewer.ScrollToVerticalOffset(scrollviewer.ScrollableHeight);
        }
    }
}