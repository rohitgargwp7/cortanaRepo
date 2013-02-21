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
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        private GeoCoordinateWatcher watcher;
        private Pushpin locationPushpin;
        private ApplicationBar appBar;
        ApplicationBarIconButton shareIconButton = null;


        public ShareLocation()
        {
            InitializeComponent();
            locationPushpin = new Pushpin();
            //locationPushpin.Style = this.Resources["PushpinStyle"] as Style;

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            //add icon for send
            shareIconButton = new ApplicationBarIconButton();
            shareIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            shareIconButton.Text = AppResources.ShareLocation_Txt;
            shareIconButton.Click += new EventHandler(shareBtn_Click);
            shareIconButton.IsEnabled = false;
            appBar.Buttons.Add(shareIconButton);

            shareLocation.ApplicationBar = appBar;

            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                watcher.MovementThreshold = 20;
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();
        }

        private void shareBtn_Click(object sender, EventArgs e)
        {
            byte[] thumbnailBytes = captureThumbnail();
            JObject metadata = new JObject();
            JArray filesData = new JArray();
            JObject singleFileInfo = new JObject();
            singleFileInfo[HikeConstants.FILE_NAME] = "Location";
            singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
            singleFileInfo[HikeConstants.LATITUDE] = locationPushpin.Location.Latitude;
            singleFileInfo[HikeConstants.LONGITUDE] = locationPushpin.Location.Longitude;
            singleFileInfo[HikeConstants.ZOOM_LEVEL] = map.ZoomLevel;
            singleFileInfo[HikeConstants.LOCATION_ADDRESS] = "";

            filesData.Add(singleFileInfo.ToObject<JToken>());

            metadata[HikeConstants.FILES_DATA] = filesData;

            object[] locationDetails = new object[2];
            locationDetails[0] = metadata;
            locationDetails[1] = thumbnailBytes;
            PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION] = locationDetails;
            map = null;
            NavigationService.GoBack();
        }

        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    MessageBox.Show(AppResources.ShareLocation_LocationServiceNotEnabled_Txt);
                    break;

                case GeoPositionStatus.NoData:
                    MessageBox.Show(AppResources.ShareLocation_LocationServiceWorking_Txt);
                    break;
            }
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown)
            {
                MessageBox.Show(AppResources.ShareLocation_PlsWaitForPosition_Txt);
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

        private byte[] captureThumbnail()
        {
            byte[] thumbnailBytes = null;
            WriteableBitmap screenshot = new WriteableBitmap(map, new TranslateTransform());
            using (MemoryStream ms = new MemoryStream())
            {
                screenshot.SaveJpeg(ms, HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH, HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT,
                    0, 47);
                thumbnailBytes = ms.ToArray();
            }
            return thumbnailBytes;
        }

        private void map_MapResolved(object sender, EventArgs e)
        {
            shareIconButton.IsEnabled = true;
            map.MapResolved -= map_MapResolved;
            watcher.StatusChanged -= watcher_StatusChanged;
            watcher.PositionChanged -= watcher_PositionChanged;
            watcher.Stop();
            watcher.Dispose();
            shellProgress.IsVisible = false;
        }
    }
}