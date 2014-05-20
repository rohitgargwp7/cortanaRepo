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
using windows_client.utils;

namespace windows_client.Controls
{
    public class MyRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MyRichTextBox), new PropertyMetadata(default(string), TextPropertyChanged));

        public static readonly DependencyProperty TypeProperty =
           DependencyProperty.Register("LinkifyAll", typeof(bool), typeof(MyRichTextBox), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("TextForeground", typeof(SolidColorBrush), typeof(MyRichTextBox), new PropertyMetadata(default(SolidColorBrush)));

        public static readonly DependencyProperty MaxCharsPerLineProperty =
            DependencyProperty.Register("MaxCharsPerLine", typeof(Int32), typeof(MyRichTextBox), new PropertyMetadata(default(Int32)));

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
        
        public SolidColorBrush TextForeground
        {
            get
            {
                return (SolidColorBrush)GetValue(ColorProperty);
            }
            set
            {
                SetValue(ColorProperty, value);
            }
        }

        public Int32 MaxCharsPerLine
        {
            get
            {
                return (Int32)GetValue(MaxCharsPerLineProperty);
            }
            set
            {
                SetValue(MaxCharsPerLineProperty, value);
            }
        }

        private static void TextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            try
            {
                var richTextBox = (MyRichTextBox)dependencyObject;
                var text = (string)dependencyPropertyChangedEventArgs.NewValue;

                richTextBox.LinkifyText(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        internal void LinkifyText(string text)
        {
            if (text == lastText)
                return;
            lastText = text;

            if (MaxCharsPerLine > 0)
            {
                var maxChar = Utils.GetMaxCharForBlock(text, 1, MaxCharsPerLine);
                if (text.Length > maxChar)
                    text = text.Substring(0, maxChar + 1).Trim() + "...";
            }

            var paragraph = LinkifyAll ? SmileyParser.Instance.LinkifyAllPerTextBlock(text, TextForeground, new SmileyParser.ViewMoreLinkClickedDelegate(viewMore_CallBack), new SmileyParser.HyperLinkClickedDelegate(hyperlink_CallBack)) : SmileyParser.Instance.LinkifyEmoticons(text);
            Blocks.Clear();
            Blocks.Add(paragraph);
        }

        void hyperlink_CallBack(object[] obj)
        {
            if (HyperlinkClicked != null)
                HyperlinkClicked(obj, null);
        }

        void viewMore_CallBack(object obj)
        {
            if (ViewMoreClicked != null)
                ViewMoreClicked(obj, null);
        }

        public event EventHandler<EventArgs> HyperlinkClicked;
        public event EventHandler<EventArgs> ViewMoreClicked;
    }
}
