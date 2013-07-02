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
using System.Windows.Shapes;

namespace windows_client.View
{
    public partial class ShowLocation : PhoneApplicationPage
    {
        GeoCoordinate _locationCoordinate;
        GeoCoordinate _myCoordinate;
        private ProgressIndicator _progressIndicator = null;
        private MapRoute MyMapRoute = null;
        private Route MyRoute = null;
        private RouteQuery MyRouteQuery = null;

        Boolean _isDirectionsShown = false;
        
        public ShowLocation()
        {
            InitializeComponent();
            GetCurrentCoordinate();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.LOCATION_COORDINATE);
                PhoneApplicationService.Current.State.Remove(HikeConstants.LOCATION_SEARCH);
                PhoneApplicationService.Current.State.Remove(HikeConstants.ZOOM_LEVEL);
            }
            else
            {
                PhoneApplicationService.Current.State[HikeConstants.LOCATION_DEVICE_COORDINATE] = _myCoordinate;

                if (MyMap != null)
                    PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
                else
                    PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL] = 16;
            }

            base.OnNavigatedFrom(e);
        }

        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            DrawMapMarkers();
        }

        private void RouteManeuverSelected(object sender, EventArgs e)
        {
            var list = sender as LongListSelector;

            if (list != null && list.SelectedItem != null)
            {
                object selectedObject = list.SelectedItem;
                var selectedIndex = list.ItemsSource.IndexOf(selectedObject);
                MyMap.SetView(MyRoute.Legs[0].Maneuvers[selectedIndex].StartGeoCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                list.SelectedItem = null;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _locationCoordinate = PhoneApplicationService.Current.State[HikeConstants.LOCATION_COORDINATE] as GeoCoordinate;

            if (App.IS_TOMBSTONED)
            {
                _myCoordinate = PhoneApplicationService.Current.State[HikeConstants.LOCATION_DEVICE_COORDINATE] as GeoCoordinate;
                MyMap.ZoomLevel = (double)PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL];

                if (_myCoordinate == null)
                    GetCurrentCoordinate();
                else
                {
                    (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                    GetDirections();
                }
            } 
            
            base.OnNavigatedTo(e);
        }

        private async void GetCurrentCoordinate()
        {
            ShowProgressIndicator();
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;

            try
            {
                Geoposition currentPosition = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));

                _myCoordinate = new GeoCoordinate(currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude);

                Dispatcher.BeginInvoke(() =>
                {
                    (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                    MyMap.SetView(_myCoordinate, 16, MapAnimationKind.Parabolic);
                    DrawMapMarkers();
                });
            }
            catch (Exception ex)
            {
                // Couldn't get current location - location might be disabled in settings
                //MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine("Location exception GetCurrentCoordinate : " + ex.StackTrace);
            }

            GetDirections();
        }

        private void ShowProgressIndicator()
        {
            if (_progressIndicator == null)
            {
                _progressIndicator = new ProgressIndicator();
                _progressIndicator.IsIndeterminate = true;
            }

            _progressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
        }

        private void HideProgressIndicator()
        {
            _progressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
        }

        private void Direction_Click(object sender, EventArgs e)
        {
            _isDirectionsShown = !_isDirectionsShown;
            
            if (_isDirectionsShown)
            {
                // Center map on the starting point (phone location) and zoom quite close
                MyMap.SetView(_myCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                
                if (MyRoute == null)
                    GetDirections();
                
                ShowDirections();
            }
            else
                HideDirections();

            DrawMapMarkers();
        }

        private void GetDirections()
        {
            // Clear the map before before making the query
            if (MyMapRoute != null)
                MyMap.RemoveRoute(MyMapRoute);
            
            MyMap.Layers.Clear();

            List<GeoCoordinate> routeCoordinates = new List<GeoCoordinate>();
            routeCoordinates.Add(_myCoordinate);
            routeCoordinates.Add(_locationCoordinate);
            CalculateRoute(routeCoordinates);
        }
        
        private void CalculateRoute(List<GeoCoordinate> route)
        {
            ShowProgressIndicator();
            MyRouteQuery = new RouteQuery();
            MyRouteQuery.TravelMode = TravelMode.Driving;
            MyRouteQuery.Waypoints = route;
            MyRouteQuery.QueryCompleted += RouteQuery_QueryCompleted;
            MyRouteQuery.QueryAsync();
        }

        private void RouteQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            HideProgressIndicator();
            if (e.Error == null)
            {
                MyRoute = e.Result;
                MyMapRoute = new MapRoute(MyRoute);
                MyMap.AddRoute(MyMapRoute);

                // Update route information and directions
                double distanceInKm = (double)MyRoute.LengthInMeters / 1000;
                var ts = new TimeSpan(MyRoute.EstimatedDuration.Hours, MyRoute.EstimatedDuration.Minutes, 0);

                timeDestination.Text = ts.ToString("hh\\:mm", System.Globalization.CultureInfo.CurrentUICulture);
                distanceDestination.Text = distanceInKm.ToString("0.0") + " km";

                List<string> routeInstructions = new List<string>();
                foreach (RouteLeg leg in MyRoute.Legs)
                {
                    for (int i = 0; i < leg.Maneuvers.Count; i++)
                    {
                        RouteManeuver maneuver = leg.Maneuvers[i];
                        string instructionText = maneuver.InstructionText;
                        distanceInKm = 0;

                        if (i > 0)
                        {
                            distanceInKm = (double)leg.Maneuvers[i - 1].LengthInMeters / 1000;
                            instructionText += " (" + distanceInKm.ToString("0.0") + " km)";
                        }

                        routeInstructions.Add(instructionText);
                    }
                }

                directionList.ItemsSource = routeInstructions;

                if (_isDirectionsShown)
                {
                    // Center map on the starting point (phone location) and zoom quite close
                    MyMap.SetView(_myCoordinate, MyMap.ZoomLevel, MapAnimationKind.Parabolic);
                }
                else
                {
                    // Center map and zoom so that whole route is visible
                    MyMap.SetView(MyRoute.Legs[0].BoundingBox, MapAnimationKind.Parabolic);
                }

                MyRouteQuery.Dispose();
            }
        }

        private void DrawMapMarkers()
        {
            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            // Draw marker for current position
            if (_myCoordinate != null)
            {
                DrawMapMarker(_myCoordinate, Colors.Orange, mapLayer, true);

                if (_locationCoordinate != null)
                    DrawMapMarker(_locationCoordinate, Colors.Red, mapLayer, true);
            }

            // Draw markers for possible waypoints when directions are shown.
            // Start and end points are already drawn with different colors.
            if (_isDirectionsShown && MyRoute != null && MyRoute.LengthInMeters > 0)
            {
                for (int i = 1; i < MyRoute.Legs[0].Maneuvers.Count - 1; i++)
                {
                    if (MyRoute.Legs[0].Maneuvers[i].StartGeoCoordinate != _myCoordinate || MyRoute.Legs[0].Maneuvers[i].StartGeoCoordinate != _locationCoordinate)
                        DrawMapMarker(MyRoute.Legs[0].Maneuvers[i].StartGeoCoordinate, (Color)Application.Current.Resources["PhoneAccentColor"], mapLayer, false);
                }
            }

            MyMap.Layers.Add(mapLayer);
        }

        private void DrawMapMarker(GeoCoordinate coordinate, Color color, MapLayer mapLayer, Boolean isPin)
        {
            MapOverlay overlay = new MapOverlay();

            if (isPin)
            {
                // Create a map marker
                Polygon polygon = new Polygon();
                polygon.Points.Add(new Point(0, 0));
                polygon.Points.Add(new Point(0, 55));
                polygon.Points.Add(new Point(25, 25));
                polygon.Points.Add(new Point(25, 0));
                polygon.Fill = new SolidColorBrush(color);

                // Enable marker to be tapped for location information
                polygon.Tag = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);

                // Create a MapOverlay and add marker.
                overlay.Content = polygon;
                overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
                overlay.PositionOrigin = new Point(0.0, 1.0);
            }
            else
            {
                Ellipse ellipse = new Ellipse();
                ellipse.Height = MyMap.ZoomLevel;
                ellipse.Width = MyMap.ZoomLevel;
                ellipse.Fill = new SolidColorBrush(Colors.White);
                ellipse.Stroke = new SolidColorBrush(color);
                overlay.Content = ellipse;
                overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
                overlay.PositionOrigin = new Point(0.0, 0.0);
            }

            mapLayer.Add(overlay);
        }

        private void HideDirections()
        {
            _isDirectionsShown = false;
            LayoutRoot.RowDefinitions[1].Height = new GridLength();
            DirectionGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowDirections()
        {
            _isDirectionsShown = true;
            LayoutRoot.RowDefinitions[0].Height = new GridLength(3, GridUnitType.Star);
            LayoutRoot.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            DirectionGrid.Visibility = Visibility.Visible;
            DrawMapMarkers();
        }

        Boolean _isMapBig = true;

        private void DirectionGrid_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            if (_isMapBig)
            {
                LayoutRoot.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                LayoutRoot.RowDefinitions[1].Height = new GridLength(3, GridUnitType.Star);
                _isMapBig = !_isMapBig;
            }
        }

        private void MyMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!_isMapBig)
            {
                LayoutRoot.RowDefinitions[0].Height = new GridLength(3, GridUnitType.Star);
                LayoutRoot.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                _isMapBig = !_isMapBig;
            }
        }
    }
}