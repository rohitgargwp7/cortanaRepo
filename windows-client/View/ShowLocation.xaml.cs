using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Device.Location;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using windows_client.Languages;
using Windows.Foundation;
using windows_client.utils;
using System.Globalization;

namespace windows_client.View
{
    public partial class ShowLocation : PhoneApplicationPage
    {
        GeoCoordinate _locationCoordinate;

        public ShowLocation()
        {
            InitializeComponent();

            MyMap.Loaded += MyMap_Loaded;
        }

        void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = HikeConstants.MICROSOFT_MAP_SERVICE_APPLICATION_ID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = HikeConstants.MICROSOFT_MAP_SERVICE_AUTHENTICATION_TOKEN;
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            PhoneApplicationService.Current.State.Remove(HikeConstants.LOCATION_MAP_COORDINATE);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                State.Remove(HikeConstants.LOCATION_SEARCH);
                State.Remove(HikeConstants.ZOOM_LEVEL);
            }
            else
            {
                if (MyMap != null)
                    State[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
                else
                    State[HikeConstants.ZOOM_LEVEL] = 16;
            }

            base.OnNavigatedFrom(e);
        }

        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            DrawMapMarkers();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _locationCoordinate = PhoneApplicationService.Current.State[HikeConstants.LOCATION_MAP_COORDINATE] as GeoCoordinate;

            if (App.IS_TOMBSTONED)
            {
                MyMap.ZoomLevel = (double)State[HikeConstants.ZOOM_LEVEL];

                return;
            }
            else if (e.NavigationMode == NavigationMode.New)
            {
                MyMap.SetView(_locationCoordinate, 16, MapAnimationKind.Parabolic);
            }

            DrawMapMarkers();

            base.OnNavigatedTo(e);
        }

        private void Direction_Click(object sender, EventArgs e)
        {
            LaunchNativeMaps(_locationCoordinate.Latitude, _locationCoordinate.Longitude);
        }
        
        private static async System.Threading.Tasks.Task LaunchNativeMaps(double latitude, double longitude)
        {
            string launchNokiaMaps = String.Format("directions://v2.0/route/destination/?latlon={0},{1}", latitude, longitude);
            await Windows.System.Launcher.LaunchUriAsync(new Uri(launchNokiaMaps));
        }

        private void DrawMapMarkers()
        {
            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            // Draw marker for current position
            if (_locationCoordinate != null)
                DrawMapMarker(_locationCoordinate, Colors.Red, mapLayer);

            MyMap.Layers.Add(mapLayer);
        }

        private void DrawMapMarker(GeoCoordinate coordinate, Color color, MapLayer mapLayer)
        {
            MapOverlay overlay = new MapOverlay();

            // Create a map marker
            Polygon polygon = new Polygon();
            polygon.Points.Add(new System.Windows.Point(0, 0));
            polygon.Points.Add(new System.Windows.Point(0, 55));
            polygon.Points.Add(new System.Windows.Point(25, 25));
            polygon.Points.Add(new System.Windows.Point(25, 0));
            polygon.Fill = new SolidColorBrush(color);

            // Enable marker to be tapped for location information
            polygon.Tag = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);

            // Create a MapOverlay and add marker.
            overlay.Content = polygon;
            overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            overlay.PositionOrigin = new System.Windows.Point(0.0, 1.0);

            mapLayer.Add(overlay);
        }
    }
}