using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        ProgressBar pBar;
        bool isShown;

        public ProgressIndicatorControl()
        {
            overlayrectangle = new Rectangle();
            overlayrectangle.Fill = (SolidColorBrush)App.Current.Resources["HikeBlack"];
            overlayrectangle.Visibility = Visibility.Collapsed;
            overlayrectangle.Opacity = 0.95;

            spProgress = new StackPanel();
            spProgress.Visibility = Visibility.Collapsed;
            spProgress.HorizontalAlignment = HorizontalAlignment.Stretch;
            spProgress.VerticalAlignment = VerticalAlignment.Center;

            txtProgressText = new TextBlock();
            txtProgressText.Margin = new Thickness(25, 0, 25, 0);
            txtProgressText.TextWrapping = TextWrapping.Wrap;
            txtProgressText.HorizontalAlignment = HorizontalAlignment.Center;
            txtProgressText.TextAlignment = TextAlignment.Center;
            txtProgressText.Foreground = UI_Utils.Instance.White;
            spProgress.Children.Add(txtProgressText);

            pBar = new ProgressBar();
            pBar.Foreground = UI_Utils.Instance.White;
            pBar.IsIndeterminate = true;
            pBar.VerticalAlignment = VerticalAlignment.Center;
            pBar.HorizontalAlignment = HorizontalAlignment.Stretch;
            pBar.Height = 10;
            pBar.FontSize = 24;
            spProgress.Children.Add(pBar);
        }

        public void Show(Grid grid, string text)
        {
            if (grid == null)
                return;//not checked for text, as only perf bar required to show in some case

            if (isShown)
                return;//so that if grid is already shown then can not be called again until hiding

            Grid.SetRow(overlayrectangle, 0);
            Grid.SetRowSpan(overlayrectangle, grid.RowDefinitions.Count);
            overlayrectangle.Visibility = Visibility.Visible;
            grid.Children.Add(overlayrectangle);

            spProgress.Visibility = Visibility.Visible;
            Grid.SetRow(spProgress, 0);
            Grid.SetRowSpan(spProgress, grid.RowDefinitions.Count);
            grid.Children.Add(spProgress);

            txtProgressText.Text = text;
            pBar.IsIndeterminate = true;
            isShown = true;

            grid.IsHitTestVisible = false;
        }

        public void Hide(Grid grid)
        {
            if (grid == null)
                return;

            overlayrectangle.Visibility = Visibility.Collapsed;
            grid.Children.Remove(overlayrectangle);

            spProgress.Visibility = Visibility.Collapsed;
            grid.Children.Remove(spProgress);

            pBar.IsIndeterminate = false;
            isShown = false;

            grid.IsHitTestVisible = true;
        }
    }
}
