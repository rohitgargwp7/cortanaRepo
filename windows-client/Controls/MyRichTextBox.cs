using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace windows_client.Controls
{
    public class MyRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MyRichTextBox), new PropertyMetadata(default(string), TextPropertyChanged));

        public static readonly DependencyProperty TypeProperty =
           DependencyProperty.Register("LinkifyAll", typeof(bool), typeof(MyRichTextBox), new PropertyMetadata(default(bool)));

        private string lastText = string.Empty;
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        public bool LinkifyAll
        {
            get
            {
                return (bool)GetValue(TypeProperty);
            }
            set
            {
                SetValue(TypeProperty, value);
            }
        }

        private static void TextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            try
            {
                var richTextBox = (MyRichTextBox)dependencyObject;
                var text = (string)dependencyPropertyChangedEventArgs.NewValue;

                if (text == richTextBox.lastText)
                    return;
                richTextBox.lastText = text;

                var paragraph = richTextBox.LinkifyAll ? SmileyParser.Instance.LinkifyAll(text) : SmileyParser.Instance.LinkifyEmoticons(text);
                richTextBox.Blocks.Clear();
                richTextBox.Blocks.Add(paragraph);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
