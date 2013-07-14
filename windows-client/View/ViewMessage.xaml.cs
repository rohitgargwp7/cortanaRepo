using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;

namespace windows_client.View
{
    public partial class ViewMessage : PhoneApplicationPage
    {
        public ViewMessage()
        {
            InitializeComponent();
            ShowMessage();
        }

        void ShowMessage()
        {
            Object messageObj;
            if (PhoneApplicationService.Current.State.TryGetValue("message", out messageObj))
            {
                PhoneApplicationService.Current.State.Remove("message");
                SolidColorBrush phoneForeground = new SolidColorBrush((Color)Application.Current.Resources["PhoneForegroundColor"]);
                string message = (string)messageObj;
                string tempString = message;
                while (tempString.Length > 0)
                {
                    int maxChar = GetMaxCharForBlock(tempString);
                    string currentString = tempString.Substring(0, maxChar);
                    tempString = tempString.Substring(maxChar);
                    RichTextBox rtb = new RichTextBox() { TextWrapping = TextWrapping.Wrap };
                    rtb.Blocks.Add(SmileyParser.Instance.LinkifyAll(currentString, phoneForeground));
                    stMessage.Children.Add(rtb);
                }
            }

        }

        const int MAX_CHARS_PER_LINE = 40;
        const int MAX_LINES_PER_BLOCK = 75;
        public int GetMaxCharForBlock(string message)
        {
            string trimmedMessage = message;
            int lineCount = 1;
            int charCount = 0;
            while (trimmedMessage.Length > 0)
            {
                char[] newLineChar = new char[] { '\r', '\n' };
                int index = trimmedMessage.IndexOfAny(newLineChar);
                if (index == -1)
                {
                    string currentString = trimmedMessage;
                    charCount += currentString.Length;
                    lineCount += Convert.ToInt32(Math.Floor(currentString.Length / (double)MAX_CHARS_PER_LINE));
                    if (lineCount > MAX_LINES_PER_BLOCK)
                    {
                        charCount -= MAX_CHARS_PER_LINE * (lineCount - MAX_LINES_PER_BLOCK - 1);
                        charCount -= currentString.Length % MAX_CHARS_PER_LINE;
                    }
                    break;
                }
                else if (index == 0)
                {
                    trimmedMessage = trimmedMessage.Substring(index + 1);
                    lineCount += 1;
                    charCount += 1;
                    if (lineCount > MAX_LINES_PER_BLOCK)
                        break;
                }
                else
                {
                    string currentString = trimmedMessage.Substring(0, index - 1);
                    charCount += currentString.Length + 2;
                    trimmedMessage = trimmedMessage.Substring(index + 1);
                    lineCount += 1 + Convert.ToInt32(Math.Floor(currentString.Length / (double)MAX_CHARS_PER_LINE));
                    if (lineCount > MAX_LINES_PER_BLOCK)
                    {
                        charCount -= (lineCount - MAX_LINES_PER_BLOCK - 2) > 0 ? MAX_CHARS_PER_LINE * (lineCount - MAX_LINES_PER_BLOCK - 2) : 0;
                        charCount -= currentString.Length % MAX_CHARS_PER_LINE;
                        break;
                    }
                }
            }
            return charCount;
        }

    }
}