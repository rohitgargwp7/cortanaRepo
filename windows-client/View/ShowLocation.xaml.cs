﻿using System;
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
        GeoCoordinate _myCoordinate;
        private ProgressIndicator _progressIndicator = null;
        private MapRoute MyMapRoute = null;
        private Route MyRoute = null;
        private RouteQuery MyRouteQuery = null;
        Geolocator _geolocator;
        Boolean _isDirectionsShown = false;
        Boolean _isLocationEnabled = true;
        Boolean _isInitialLoad = true;
        Boolean _isSameLocation = false;

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
                if (MyMap != null)
                    PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
                else
                    PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL] = 16;
            }

            if (_myCoordinate != null)
                App.WriteToIsoStorageSettings(HikeConstants.LOCATION_DEVICE_COORDINATE, _myCoordinate);

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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_geolocator == null)
                _geolocator = new Geolocator();

            if (_geolocator.LocationStatus == PositionStatus.Disabled)
            {
                var result = MessageBox.Show(AppResources.ShareLocation_LocationServiceNotEnabled_Txt, AppResources.Location_Heading, MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));

                _isLocationEnabled = false;
            }
            else if (App.appSettings.TryGetValue<bool>(App.USE_LOCATION_SETTING, out _isLocationEnabled))
            {
                var result = MessageBox.Show(AppResources.ShareLocation_LocationSettingsNotEnabled_Txt, AppResources.Location_Heading, MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    App.appSettings.Remove(App.USE_LOCATION_SETTING);
                    App.appSettings.Save();
                    _isLocationEnabled = true;
                }
            }
            else
                _isLocationEnabled = true;

            _locationCoordinate = PhoneApplicationService.Current.State[HikeConstants.LOCATION_COORDINATE] as GeoCoordinate;
            App.appSettings.TryGetValue(HikeConstants.LOCATION_DEVICE_COORDINATE, out _myCoordinate);

            if (!_isLocationEnabled)
            {
                ApplicationBar.IsVisible = false;
                _isInitialLoad = false;

                if (MyMapRoute != null)
                    MyMap.RemoveRoute(MyMapRoute);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MyMap.SetView(_locationCoordinate, 16, MapAnimationKind.Parabolic);
                    DrawMapMarkers();
                });

                return;
            }

            if (App.IS_TOMBSTONED)
            {
                MyMap.ZoomLevel = (double)PhoneApplicationService.Current.State[HikeConstants.ZOOM_LEVEL];

                if (_myCoordinate == null)
                    GetCurrentCoordinate();
                else
                    GetDirections();

                return;
            }

            if (e.NavigationMode == NavigationMode.New)
                GetCurrentCoordinate();
            else if (MyRoute == null)
                GetDirections();
            else
                DrawMapMarkers();

            base.OnNavigatedTo(e);
        }

        private async void GetCurrentCoordinate()
        {
            if (!_isLocationEnabled)
            {
                HideProgressIndicator();
                return;
            }

            ShowProgressIndicator();
            _geolocator.DesiredAccuracyInMeters = 10;
            _geolocator.MovementThreshold = 5;
            _geolocator.DesiredAccuracy = PositionAccuracy.High;

            IAsyncOperation<Geoposition> locationTask = _geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(2));

            try
            {
                Geoposition currentPosition = await locationTask;

                _myCoordinate = new GeoCoordinate(currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude);
            }
            catch (Exception ex)
            {
                // Couldn't get current location - location might be disabled in settings
                //MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
                System.Diagnostics.Debug.WriteLine("Location exception GetCurrentCoordinate : " + ex.StackTrace);
            }
            finally
            {
                if (locationTask != null)
                {
                    if (locationTask.Status == AsyncStatus.Started)
                        locationTask.Cancel();

                    locationTask.Close();
                }

                GetDirections();
            }
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

                _isMapBig = true;

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

            _isInitialLoad = false;

            if (e.Error == null)
            {
                MyRoute = e.Result;
                MyMapRoute = new MapRoute(MyRoute);

                if (MyRoute.Legs != null && MyRoute.Legs[0].Maneuvers != null && MyRoute.Legs[0].Maneuvers.Count == 2)
                {
                    _isSameLocation = true;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        MyMap.SetView(_locationCoordinate, 16, MapAnimationKind.Parabolic);
                        DrawMapMarkers();
                    });

                    return;
                }

                MyMap.AddRoute(MyMapRoute);

                // Update route information and directions
                double distance = (double)MyRoute.LengthInMeters;
                var ts = new TimeSpan(MyRoute.EstimatedDuration.Hours, MyRoute.EstimatedDuration.Minutes, 0);

                timeDestination.Text = ts.ToString("hh\\:mm", System.Globalization.CultureInfo.CurrentUICulture);
                
                if (MyRoute.LengthInMeters > 1000)
                {
                    distance /= 1000;
                    distanceDestination.Text = String.Format(AppResources.Kilometer_Abbreviation, distance.ToString("0.0", CultureInfo.InvariantCulture));
                }
                else
                    distanceDestination.Text = String.Format(AppResources.Meter_Abbreviation, distance.ToString("0.0", CultureInfo.InvariantCulture));

                List<Direction> routeInstructions = new List<Direction>();
                
                foreach (RouteLeg leg in MyRoute.Legs)
                {
                    for (int i = 0; i < leg.Maneuvers.Count; i++)
                    {
                        RouteManeuver maneuver = leg.Maneuvers[i];
                        Direction direction = new Direction()
                        {
                            Instruction = maneuver.InstructionText,
                            InstructionKind = maneuver.InstructionKind
                        };

                        if (i > 0)
                        {
                            distance = (double)leg.Maneuvers[i - 1].LengthInMeters;

                            if (distance > 1000)
                            {
                                distance = distance / 1000;
                                direction.Distance = String.Format(AppResources.Kilometer_Abbreviation, distance.ToString("0.0", CultureInfo.InvariantCulture));
                            }
                            else
                                direction.Distance = String.Format(AppResources.Meter_Abbreviation, distance.ToString("0.0", CultureInfo.InvariantCulture));
                            
                        }

                        routeInstructions.Add(direction);
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
                this.ApplicationBar.IsVisible = true;
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MyMap.SetView(_locationCoordinate, 16, MapAnimationKind.Parabolic);
                    DrawMapMarkers();
                });
            }
        }

        private void DrawMapMarkers()
        {
            if (_isInitialLoad)
                return;

            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            // Draw marker for current position
            if (_myCoordinate != null && _isLocationEnabled && !_isSameLocation)
                DrawMapMarker(_myCoordinate, Colors.Orange, mapLayer, true);

            if (_locationCoordinate != null)
                DrawMapMarker(_locationCoordinate, Colors.Red, mapLayer, true);

            // Draw markers for possible waypoints when directions are shown.
            // Start and end points are already drawn with different colors.
            if (_isDirectionsShown && MyRoute != null && MyRoute.LengthInMeters > 0 && _isLocationEnabled)
            {
                for (int i = 1; i < MyRoute.Legs[0].Maneuvers.Count - 1; i++)
                    DrawMapMarker(MyRoute.Legs[0].Maneuvers[i].StartGeoCoordinate, (Color)Application.Current.Resources["PhoneAccentColor"], mapLayer, false);
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
            }
            else
            {
                Ellipse ellipse = new Ellipse();
                ellipse.Height = MyMap.ZoomLevel + 4;
                ellipse.Width = MyMap.ZoomLevel + 4;
                ellipse.Fill = new SolidColorBrush(Colors.White);
                ellipse.Stroke = new SolidColorBrush(color);
                overlay.Content = ellipse;
                overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
                overlay.PositionOrigin = new System.Windows.Point(0.5, 0.5);
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
            LayoutRoot.RowDefinitions[0].Height = new GridLength(1.5, GridUnitType.Star);
            LayoutRoot.RowDefinitions[1].Height = new GridLength(2.5, GridUnitType.Star);
            _isMapBig = false;
            DirectionGrid.Visibility = Visibility.Visible;
        }

        Boolean _isMapBig = true;

        private void DirectionGrid_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            if (_isMapBig)
            {
                LayoutRoot.RowDefinitions[0].Height = new GridLength(1.5, GridUnitType.Star);
                LayoutRoot.RowDefinitions[1].Height = new GridLength(2.5, GridUnitType.Star);
                _isMapBig = !_isMapBig;
            }
        }

        private void MyMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!_isMapBig)
            {
                LayoutRoot.RowDefinitions[0].Height = new GridLength(2.5, GridUnitType.Star);
                LayoutRoot.RowDefinitions[1].Height = new GridLength(1.5, GridUnitType.Star);
                _isMapBig = !_isMapBig;
            }
        }
    }

    public class Direction
    {
        public string Instruction { get; set; }
        
        public RouteManeuverInstructionKind InstructionKind { get; set; }

        public BitmapImage DirectionImage
        {
            get
            {
                return InstructionKindToImage();
            }
        }
        
        public string Distance { get; set; }

        BitmapImage InstructionKindToImage()
        {
            switch (InstructionKind)
            {
                case RouteManeuverInstructionKind.End: 
                    return DirectionImageUtils.Instance.InstructionKindEnd;
                case RouteManeuverInstructionKind.FreewayContinueLeft:
                    return DirectionImageUtils.Instance.InstructionKindFreewayContinueLeft;
                case RouteManeuverInstructionKind.FreewayContinueRight:
                    return DirectionImageUtils.Instance.InstructionKindFreewayContinueRight;
                case RouteManeuverInstructionKind.FreewayEnterLeft:
                    return DirectionImageUtils.Instance.InstructionKindFreewayEnterLeft;
                case RouteManeuverInstructionKind.FreewayEnterRight:
                    return DirectionImageUtils.Instance.InstructionKindFreewayEnterRight;
                case RouteManeuverInstructionKind.FreewayLeaveLeft:
                    return DirectionImageUtils.Instance.InstructionKindFreewayLeaveLeft;
                case RouteManeuverInstructionKind.FreewayLeaveRight:
                    return DirectionImageUtils.Instance.InstructionKindFreewayLeaveRight;
                case RouteManeuverInstructionKind.GoStraight:
                    return DirectionImageUtils.Instance.InstructionKindGoStraight;
                case RouteManeuverInstructionKind.Start:
                    return DirectionImageUtils.Instance.InstructionKindStart;
                case RouteManeuverInstructionKind.Stopover: 
                    return DirectionImageUtils.Instance.InstructionKindStopover;
                case RouteManeuverInstructionKind.StopoverResume:
                    return DirectionImageUtils.Instance.InstructionKindStopoverResume;
                case RouteManeuverInstructionKind.TakeFerry:
                    return DirectionImageUtils.Instance.InstructionKindTakeFerry;
                case RouteManeuverInstructionKind.TrafficCircleLeft:
                    return DirectionImageUtils.Instance.InstructionKindTrafficCircleLeft;
                case RouteManeuverInstructionKind.TrafficCircleRight:
                    return DirectionImageUtils.Instance.InstructionKindTrafficCircleRight;
                case RouteManeuverInstructionKind.TurnHardLeft:
                    return DirectionImageUtils.Instance.InstructionKindTurnHardLeft;
                case RouteManeuverInstructionKind.TurnHardRight:
                    return DirectionImageUtils.Instance.InstructionKindTurnHardRight;
                case RouteManeuverInstructionKind.TurnKeepLeft:
                    return DirectionImageUtils.Instance.InstructionKindTurnKeepLeft;
                case RouteManeuverInstructionKind.TurnKeepRight:
                    return DirectionImageUtils.Instance.InstructionKindTurnKeepRight;
                case RouteManeuverInstructionKind.TurnLeft:
                    return DirectionImageUtils.Instance.InstructionKindTurnLeft;
                case RouteManeuverInstructionKind.TurnLightLeft:
                    return DirectionImageUtils.Instance.InstructionKindTurnLightLeft;
                case RouteManeuverInstructionKind.TurnLightRight:
                    return DirectionImageUtils.Instance.InstructionKindTurnLightRight;
                case RouteManeuverInstructionKind.TurnRight:
                    return DirectionImageUtils.Instance.InstructionKindTurnRight;
                case RouteManeuverInstructionKind.Undefined:
                    return DirectionImageUtils.Instance.InstructionKindUndefined;
                case RouteManeuverInstructionKind.UTurnLeft:
                    return DirectionImageUtils.Instance.InstructionKindUTurnLeft;
                case RouteManeuverInstructionKind.UTurnRight: 
                    return DirectionImageUtils.Instance.InstructionKindUTurnRight;
            }
            return new BitmapImage();
        }

        public Visibility DistanceVisibility
        {
            get
            {
                return String.IsNullOrEmpty(Distance) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}