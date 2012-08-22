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
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;

namespace windows_client.Controls
{
    public class MyChatBubble : UserControl {

        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyChatBubble), new PropertyMetadata(""));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static DependencyProperty TimeStampProperty = DependencyProperty.Register("TimeStamp", typeof(DateTime), typeof(MyChatBubble), new PropertyMetadata(DateTime.Now));

        public DateTime TimeStamp {
            get { return (DateTime)GetValue(TimeStampProperty); }
            set {
                if (value > DateTime.Now) value = DateTime.Now;
                SetValue(TimeStampProperty, value); 
            }
        }

        public MyChatBubble() 
        {
        }

        public MyChatBubble(RoutedEventHandler copyClick, RoutedEventHandler forwardClick)
        {
            ContextMenu menu = new ContextMenu();
            menu.IsZoomEnabled = false;

            MenuItem copy = new MenuItem();
            copy.Header = "copy";
            copy.Click += copyClick;
            menu.Items.Add(copy);
            ContextMenuService.SetContextMenu(this, menu);

            MenuItem forward = new MenuItem();
            forward.Header = "forward";
            forward.Click += forwardClick;
            menu.Items.Add(forward);
            ContextMenuService.SetContextMenu(this, menu);
        
        }

        void AddButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = (sender is MyChatBubble);
            int i = 32;
            i++;
        }


        void MenuItem_Tap_Copy(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Clipboard.SetText(this.Text);
        }

        //private void MenuItem_Tap_Forward(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    PhoneApplicationService.Current.State["forwardedText"] = this.Text;
        //    NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Absolute));
        //}

    }
}
