using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Device.Location;
using System.Windows.Input;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Tasks;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        private GeoCoordinateWatcher watcher;
        private Pushpin locationPushpin;
        public ShareLocation()
        {
            InitializeComponent();
            locationPushpin = new Pushpin();
            //locationPushpin.Style = this.Resources["PushpinStyle"] as Style;
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                watcher.MovementThreshold = 20;
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();
        }
        private void startLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                watcher.MovementThreshold = 20;
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();
        }

        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    MessageBox.Show("Location Service is not enabled on the device");
                    break;

                case GeoPositionStatus.NoData:
                    MessageBox.Show(" The Location Service is working, but it cannot get location data.");
                    break;
            }
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown)
            {
                MessageBox.Show("Please wait while your prosition is determined....");
                return;
            }

            this.map.Center = new GeoCoordinate(e.Position.Location.Latitude, e.Position.Location.Longitude);

            if (this.map.Children.Contains(locationPushpin))
                this.map.Children.Remove(locationPushpin);

            locationPushpin.Tag = "locationPushpin";
            locationPushpin.Location = watcher.Position.Location;
            this.map.Children.Add(locationPushpin);
            this.map.SetView(watcher.Position.Location, 18.0);
        }

        private void map_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Point p = e.GetPosition(this.map);
            GeoCoordinate geo = new GeoCoordinate();
            geo = map.ViewportPointToLocation(p);
            //map.ZoomLevel = 17;
            map.Center = geo;
            locationPushpin.Location = geo;
            if (this.map.Children.Contains(locationPushpin))
                this.map.Children.Remove(locationPushpin);

            //---add the pushpin to the map---
            map.Children.Add(locationPushpin);
            
        }

        //private void captureIMage_Click(object sender, RoutedEventArgs e)
        //{
        //    int Width = (int)map.RenderSize.Width;
        //    int Height = (int)map.RenderSize.Height;

        //    // Write the map control to a WriteableBitmap 
        //    WriteableBitmap screenshot = new WriteableBitmap(map, new TranslateTransform());
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        // Save it to a memory stream 
        //        screenshot.SaveJpeg(ms, Width, Height, 0, 100);

        //        // Take saved memory stream and put it back into an BitmapImage 
        //        BitmapImage img = new BitmapImage();
        //        img.SetSource(ms);

        //        // Cleanup 
        //        ms.Close();
        //    }
        //    //this.map.Visibility = Visibility.Collapsed;

        //}

    }
}