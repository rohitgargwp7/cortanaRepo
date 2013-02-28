using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using windows_client.Languages;
using windows_client.utils;

namespace windows_client.Controls
{
    class ProgressIndicatorControl
    {
        Rectangle overlayrectangle;
        StackPanel spProgress;
        TextBlock txtProgressText;
        PerformanceProgressBar pBar;
        public ProgressIndicatorControl(Grid grid, string text)
        {
            overlayrectangle = new Rectangle();
            Grid.SetRow(overlayrectangle, 0);
            Grid.SetRowSpan(overlayrectangle, grid.RowDefinitions.Count);
            overlayrectangle.Fill = UI_Utils.Instance.Black;
            overlayrectangle.Visibility = Visibility.Collapsed;
            grid.Children.Add(overlayrectangle);

            spProgress = new StackPanel();
            Grid.SetRow(spProgress, 0);
            overlayrectangle.Visibility = Visibility.Collapsed;
            grid.Children.Add(spProgress);

            txtProgressText = new TextBlock();
            txtProgressText.Margin = new Thickness(24, 250, 24, 0);
            txtProgressText.TextWrapping = TextWrapping.Wrap;
            txtProgressText.HorizontalAlignment = HorizontalAlignment.Center;
            txtProgressText.TextAlignment = TextAlignment.Center;
            spProgress.Children.Add(txtProgressText);

            pBar = new PerformanceProgressBar();
            pBar.IsEnabled = false;
            //todo:transparent  pBar.Background=UI_Utils.Instance.
            pBar.IsIndeterminate = true;
            pBar.VerticalAlignment = VerticalAlignment.Bottom;
            pBar.VerticalContentAlignment = VerticalAlignment.Center;
            pBar.HorizontalAlignment = HorizontalAlignment.Stretch;
            pBar.Height = 10;
            pBar.FontSize = 24;
            pBar.HorizontalContentAlignment = HorizontalAlignment.Center;
            pBar.IsTabStop = true;
            pBar.Opacity = 0;
            spProgress.Children.Add(pBar);

        }

        public void Show()
        {
            txtProgressText.Text = AppResources.SelectUser_RefreshWaitMsg_Txt;
            overlayrectangle.Visibility = System.Windows.Visibility.Visible;
            overlayrectangle.Opacity = 0.85;
            spProgress.Visibility = Visibility.Visible;
            pBar.Opacity = 1;
            pBar.IsEnabled = true;
        }

        public void Hide()
        {
            overlayrectangle.Visibility = System.Windows.Visibility.Collapsed;
            spProgress.Visibility = Visibility.Collapsed;
            pBar.Opacity = 0;
            pBar.IsEnabled = false;
        }
    }
}
