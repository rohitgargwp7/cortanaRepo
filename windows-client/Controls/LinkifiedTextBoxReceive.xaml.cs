using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class LinkifiedTextBoxReceive : UserControl
    {
        public static DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LinkifiedTextBoxReceive),
            new PropertyMetadata("", ChangedText));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void ChangedText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinkifiedTextBoxReceive)d).ChangedText(e);
        }

        private void ChangedText(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                Paragraph richtext = UI_Utils.Instance.Linkify((string)e.NewValue);
                RichText.Blocks.Add(richtext);
            }
        }

        public LinkifiedTextBoxReceive()
        {
            InitializeComponent();
        }
    }
}
