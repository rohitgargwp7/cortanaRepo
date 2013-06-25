using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Device.Location;
using System.Windows.Input;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Windows.Devices.Geolocation;
using System.Collections.Generic;
using System.Windows.Shapes;

namespace windows_client.View
{
    public partial class ShareLocation : PhoneApplicationPage
    {
        GeoCoordinate locationToBeShared;
        ApplicationBarIconButton shareIconButton = null;

        // Application bar menu items
        private ApplicationBarMenuItem AppBarColorModeMenuItem = null;
        private ApplicationBarMenuItem AppBarLandmarksMenuItem = null;
        private ApplicationBarMenuItem AppBarPedestrianFeaturesMenuItem = null;
        private ApplicationBarMenuItem AppBarDirectionsMenuItem = null;
        private ApplicationBarMenuItem AppBarAboutMenuItem = null;

        // Progress indicator shown in system tray
        private ProgressIndicator ProgressIndicator = null;

        // My current location
        private GeoCoordinate MyCoordinate = null;

        // List of coordinates representing search hits / destination of route
        private List<GeoCoordinate> MyCoordinates = new List<GeoCoordinate>();

        // Geocode query
        private GeocodeQuery MyGeocodeQuery = null;

        // Route query
        private RouteQuery MyRouteQuery = null;

        // Reverse geocode query
        private ReverseGeocodeQuery MyReverseGeocodeQuery = null;

        // Route information
        private Route MyRoute = null;

        // Route overlay on map
        private MapRoute MyMapRoute = null;

        /// <summary>
        /// True when this object instance has been just created, otherwise false
        /// </summary>
        private bool _isNewInstance = true;

        /// <summary>
        /// True when access to user location is allowed, otherwise false
        /// </summary>
        private bool _isLocationAllowed = false;

        /// <summary>
        /// True when color mode has been temporarily set to light, otherwise false
        /// </summary>
        private bool _isTemporarilyLight = false;

        /// <summary>
        /// True when route is being searched, otherwise false
        /// </summary>
        private bool _isRouteSearch = false;

        /// <summary>
        /// True when directions are shown, otherwise false
        /// </summary>
        private bool _isDirectionsShown = false;

        /// <summary>
        /// Travel mode used when calculating route
        /// </summary>
        private TravelMode _travelMode = TravelMode.Driving;

        /// <summary>
        /// Accuracy of my current location in meters;
        /// </summary>
        private double _accuracy = 0.0;

        private void BuildApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.    
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsMenuEnabled = true;

            // Create new buttons with the localized strings from AppResources.
            ApplicationBarIconButton appBarSearchButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.feature.search.rest.png", UriKind.Relative));
            appBarSearchButton.Text = "Search";
            appBarSearchButton.Click += new EventHandler(Search_Click);
            ApplicationBar.Buttons.Add(appBarSearchButton);

            ApplicationBarIconButton appBarRouteButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.show.route.png", UriKind.Relative));
            appBarRouteButton.Text = "route";
            appBarRouteButton.Click += new EventHandler(Route_Click);
            ApplicationBar.Buttons.Add(appBarRouteButton);

            ApplicationBarIconButton appBarLocateMeButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.locate.me.png", UriKind.Relative));
            appBarLocateMeButton.Text = "My location";
            appBarLocateMeButton.Click += new EventHandler(LocateMe_Click);
            ApplicationBar.Buttons.Add(appBarLocateMeButton);

            shareIconButton = new ApplicationBarIconButton();
            shareIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            shareIconButton.Text = AppResources.ShareLocation_Txt;
            shareIconButton.Click += new EventHandler(shareBtn_Click);
            shareIconButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(shareIconButton);

            // Create new menu items with the localized strings from AppResources.
            AppBarColorModeMenuItem = new ApplicationBarMenuItem("ColorModeDarkMenuItemText");
            AppBarColorModeMenuItem.Click += new EventHandler(ColorMode_Click);
            ApplicationBar.MenuItems.Add(AppBarColorModeMenuItem);

            AppBarLandmarksMenuItem = new ApplicationBarMenuItem("LandmarksOnMenuItemText");
            AppBarLandmarksMenuItem.Click += new EventHandler(Landmarks_Click);
            ApplicationBar.MenuItems.Add(AppBarLandmarksMenuItem);

            AppBarPedestrianFeaturesMenuItem = new ApplicationBarMenuItem("PedestrianFeaturesOnMenuItemText");
            AppBarPedestrianFeaturesMenuItem.Click += new EventHandler(PedestrianFeatures_Click);
            ApplicationBar.MenuItems.Add(AppBarPedestrianFeaturesMenuItem);

            AppBarDirectionsMenuItem = new ApplicationBarMenuItem("DirectionsOnMenuItemText");
            AppBarDirectionsMenuItem.Click += new EventHandler(Directions_Click);
            AppBarDirectionsMenuItem.IsEnabled = false;
            ApplicationBar.MenuItems.Add(AppBarDirectionsMenuItem);
        }

        private void Search_Click(object sender, EventArgs e)
        {
            HideDirections();
            _isRouteSearch = false;
            SearchTextBox.SelectAll();
            SearchTextBox.Visibility = Visibility.Visible;
            SearchTextBox.Focus();
        }

        private void Route_Click(object sender, EventArgs e)
        {
            HideDirections();

            if (!_isLocationAllowed)
            {
                MessageBoxResult result = MessageBox.Show("NO Location allowed","ALLOW",
                                                          MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    _isLocationAllowed = true;
                    //SaveSettings();
                    GetCurrentCoordinate();
                }
            }
            else if (MyCoordinate == null)
            {
                MessageBox.Show("No Current Location", "Error", MessageBoxButton.OK);
            }
            else
            {
                _isRouteSearch = true;
                SearchTextBox.SelectAll();
                SearchTextBox.Visibility = Visibility.Visible;
                SearchTextBox.Focus();
            }
        }

        private void LocateMe_Click(object sender, EventArgs e)
        {
            if (_isLocationAllowed)
            {
                GetCurrentCoordinate();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("NO LOCATION ALLOWED",
                                                          "",
                                                          MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    _isLocationAllowed = true;
                    //SaveSettings();
                    GetCurrentCoordinate();
                }
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SearchTextBox.Text.Length > 0)
                {
                    // New query - Clear the map of markers and routes
                    if (MyMapRoute != null)
                    {
                        MyMap.RemoveRoute(MyMapRoute);
                    }
                    MyCoordinates.Clear();
                    DrawMapMarkers();

                    HideDirections();
                    AppBarDirectionsMenuItem.IsEnabled = false;

                    SearchForTerm(SearchTextBox.Text);
                    this.Focus();
                }
            }
        }

        private void SearchTextBox_LostFocus(object sender, EventArgs e)
        {
            SearchTextBox.Visibility = Visibility.Collapsed;
        }

        private void ColorMode_Click(object sender, EventArgs e)
        {
            if (MyMap.ColorMode == MapColorMode.Dark)
            {
                MyMap.ColorMode = MapColorMode.Light;
                AppBarColorModeMenuItem.Text = "Drk";
            }
            else
            {
                MyMap.ColorMode = MapColorMode.Dark;
                AppBarColorModeMenuItem.Text = "light";
            }
        }

        private void Landmarks_Click(object sender, EventArgs e)
        {
            MyMap.LandmarksEnabled = !MyMap.LandmarksEnabled;
            if (MyMap.LandmarksEnabled)
            {
                AppBarLandmarksMenuItem.Text = "Landmarks off";
            }
            else
            {
                AppBarLandmarksMenuItem.Text = "Landmarks on";
            }
        }

        private void PedestrianFeatures_Click(object sender, EventArgs e)
        {
            MyMap.PedestrianFeaturesEnabled = !MyMap.PedestrianFeaturesEnabled;
            if (MyMap.PedestrianFeaturesEnabled)
            {
                AppBarPedestrianFeaturesMenuItem.Text = "pedes off";
            }
            else
            {
                AppBarPedestrianFeaturesMenuItem.Text = "pedes on";
            }
        }

        private void Directions_Click(object sender, EventArgs e)
        {
            _isDirectionsShown = !_isDirectionsShown;
            if (_isDirectionsShown)
            {
                // Center map on the starting point (phone location) and zoom quite close
                MyMap.SetView(MyCoordinate, 16, MapAnimationKind.Parabolic);
                ShowDirections();
            }
            else
            {
                HideDirections();
            }
            DrawMapMarkers();
        }

        private void PitchValueChanged(object sender, EventArgs e)
        {
            if (PitchSlider != null)
            {
                MyMap.Pitch = PitchSlider.Value;
            }
        }

        private void HeadingValueChanged(object sender, EventArgs e)
        {
            if (HeadingSlider != null)
            {
                double value = HeadingSlider.Value;
                if (value > 360) value -= 360;
                MyMap.Heading = value;
            }
        }

        private void CartographicModeButton_Click(object sender, EventArgs e)
        {
            RoadButton.IsEnabled = true;
            AerialButton.IsEnabled = true;
            HybridButton.IsEnabled = true;
            TerrainButton.IsEnabled = true;
            AppBarColorModeMenuItem.IsEnabled = false;

            if (sender == RoadButton)
            {
                AppBarColorModeMenuItem.IsEnabled = true;
                // To change color mode back to dark
                if (_isTemporarilyLight)
                {
                    _isTemporarilyLight = false;
                    MyMap.ColorMode = MapColorMode.Dark;
                }
                MyMap.CartographicMode = MapCartographicMode.Road;
                RoadButton.IsEnabled = false;
            }
            else if (sender == AerialButton)
            {
                MyMap.CartographicMode = MapCartographicMode.Aerial;
                AerialButton.IsEnabled = false;
            }
            else if (sender == HybridButton)
            {
                MyMap.CartographicMode = MapCartographicMode.Hybrid;
                HybridButton.IsEnabled = false;
            }
            else if (sender == TerrainButton)
            {
                // To enable terrain mode when color mode is dark
                if (MyMap.ColorMode == MapColorMode.Dark)
                {
                    _isTemporarilyLight = true;
                    MyMap.ColorMode = MapColorMode.Light;
                }
                MyMap.CartographicMode = MapCartographicMode.Terrain;
                TerrainButton.IsEnabled = false;
            }
        }

        private void TravelModeButton_Click(object sender, EventArgs e)
        {
            // Clear the map before before making the query
            if (MyMapRoute != null)
            {
                MyMap.RemoveRoute(MyMapRoute);
            }
            MyMap.Layers.Clear();

            if (sender == DriveButton)
            {
                _travelMode = TravelMode.Driving;
            }
            else if (sender == WalkButton)
            {
                _travelMode = TravelMode.Walking;
            }
            DriveButton.IsEnabled = !DriveButton.IsEnabled;
            WalkButton.IsEnabled = !WalkButton.IsEnabled;

            // Route from current location to first search result
            List<GeoCoordinate> routeCoordinates = new List<GeoCoordinate>();
            routeCoordinates.Add(MyCoordinate);
            routeCoordinates.Add(MyCoordinates[0]);
            CalculateRoute(routeCoordinates);
        }

        private void RouteManeuverSelected(object sender, EventArgs e)
        {
            object selectedObject = RouteLLS.SelectedItem;
            int selectedIndex = RouteLLS.ItemsSource.IndexOf(selectedObject);
            MyMap.SetView(MyRoute.Legs[0].Maneuvers[selectedIndex].StartGeoCoordinate, 16, MapAnimationKind.Parabolic);
        }

        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            DrawMapMarkers();
        }

        private void ShowDirections()
        {
            _isDirectionsShown = true;
            AppBarDirectionsMenuItem.Text = "directions off";
            DirectionsTitleRowDefinition.Height = GridLength.Auto;
            DirectionsRowDefinition.Height = new GridLength(2, GridUnitType.Star);
            ModePanel.Visibility = Visibility.Collapsed;
            HeadingSlider.Visibility = Visibility.Collapsed;
            PitchSlider.Visibility = Visibility.Collapsed;
        }

        private void HideDirections()
        {
            _isDirectionsShown = false;
            AppBarDirectionsMenuItem.Text = "directions on";
            DirectionsTitleRowDefinition.Height = new GridLength(0);
            DirectionsRowDefinition.Height = new GridLength(0);
            ModePanel.Visibility = Visibility.Visible;
            HeadingSlider.Visibility = Visibility.Visible;
            PitchSlider.Visibility = Visibility.Visible;
        }

        private void SearchForTerm(String searchTerm)
        {
            ShowProgressIndicator("searching");
            MyGeocodeQuery = new GeocodeQuery();
            MyGeocodeQuery.SearchTerm = searchTerm;
            MyGeocodeQuery.GeoCoordinate = MyCoordinate == null ? new GeoCoordinate(0, 0) : MyCoordinate;
            MyGeocodeQuery.QueryCompleted += GeocodeQuery_QueryCompleted;
            MyGeocodeQuery.QueryAsync();
        }

        private void GeocodeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            HideProgressIndicator();
            if (e.Error == null)
            {
                if (e.Result.Count > 0)
                {
                    if (_isRouteSearch) // Query is made to locate the destination of a route
                    {
                        // Only store the destination for drawing the map markers
                        MyCoordinates.Add(e.Result[0].GeoCoordinate);

                        // Route from current location to first search result
                        List<GeoCoordinate> routeCoordinates = new List<GeoCoordinate>();
                        routeCoordinates.Add(MyCoordinate);
                        routeCoordinates.Add(e.Result[0].GeoCoordinate);
                        CalculateRoute(routeCoordinates);
                    }
                    else // Query is made to search the map for a keyword
                    {
                        // Add all results to MyCoordinates for drawing the map markers.
                        for (int i = 0; i < e.Result.Count; i++)
                        {
                            MyCoordinates.Add(e.Result[i].GeoCoordinate);
                        }

                        // Center on the first result.
                        MyMap.SetView(e.Result[0].GeoCoordinate, 10, MapAnimationKind.Parabolic);
                    }
                }
                else
                {
                    MessageBox.Show("No match found", "", MessageBoxButton.OK);
                }

                MyGeocodeQuery.Dispose();
            }
            DrawMapMarkers();
        }

        private void CalculateRoute(List<GeoCoordinate> route)
        {
            ShowProgressIndicator("Calculating Route");
            MyRouteQuery = new RouteQuery();
            MyRouteQuery.TravelMode = _travelMode;
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
                DestinationText.Text = SearchTextBox.Text;
                double distanceInKm = (double)MyRoute.LengthInMeters / 1000;
                DestinationDetailsText.Text = distanceInKm.ToString("0.0") + " km, "
                                              + MyRoute.EstimatedDuration.Hours + " hrs "
                                              + MyRoute.EstimatedDuration.Minutes + " mins.";

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
                RouteLLS.ItemsSource = routeInstructions;

                AppBarDirectionsMenuItem.IsEnabled = true;

                if (_isDirectionsShown)
                {
                    // Center map on the starting point (phone location) and zoom quite close
                    MyMap.SetView(MyCoordinate, 16, MapAnimationKind.Parabolic);
                }
                else
                {
                    // Center map and zoom so that whole route is visible
                    MyMap.SetView(MyRoute.Legs[0].BoundingBox, MapAnimationKind.Parabolic);
                }
                MyRouteQuery.Dispose();
            }
            DrawMapMarkers();
        }

        private void Marker_Click(object sender, EventArgs e)
        {
            Polygon p = (Polygon)sender;
            GeoCoordinate geoCoordinate = (GeoCoordinate)p.Tag;
            if (MyReverseGeocodeQuery == null || !MyReverseGeocodeQuery.IsBusy)
            {
                MyReverseGeocodeQuery = new ReverseGeocodeQuery();
                MyReverseGeocodeQuery.GeoCoordinate = new GeoCoordinate(geoCoordinate.Latitude, geoCoordinate.Longitude);
                MyReverseGeocodeQuery.QueryCompleted += ReverseGeocodeQuery_QueryCompleted;
                MyReverseGeocodeQuery.QueryAsync();
            }
        }

        private void ReverseGeocodeQuery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                if (e.Result.Count > 0)
                {
                    MapAddress address = e.Result[0].Information.Address;
                    String msgBoxText = "";
                    if (address.Street.Length > 0)
                    {
                        msgBoxText += "\n" + address.Street;
                        if (address.HouseNumber.Length > 0) msgBoxText += " " + address.HouseNumber;
                    }
                    if (address.PostalCode.Length > 0) msgBoxText += "\n" + address.PostalCode;
                    if (address.City.Length > 0) msgBoxText += "\n" + address.City;
                    if (address.Country.Length > 0) msgBoxText += "\n" + address.Country;
                    MessageBox.Show(msgBoxText, "", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("No info", "", MessageBoxButton.OK);
                }
                MyReverseGeocodeQuery.Dispose();
            }
        }

        private void DrawMapMarkers()
        {
            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            // Draw marker for current position
            if (MyCoordinate != null)
            {
                DrawAccuracyRadius(mapLayer);
                DrawMapMarker(MyCoordinate, Colors.Red, mapLayer);
            }

            // Draw markers for location(s) / destination(s)
            for (int i = 0; i < MyCoordinates.Count; i++)
            {
                DrawMapMarker(MyCoordinates[i], Colors.Blue, mapLayer);
            }

            // Draw markers for possible waypoints when directions are shown.
            // Start and end points are already drawn with different colors.
            if (_isDirectionsShown && MyRoute.LengthInMeters > 0)
            {
                for (int i = 1; i < MyRoute.Legs[0].Maneuvers.Count - 1; i++)
                {
                    DrawMapMarker(MyRoute.Legs[0].Maneuvers[i].StartGeoCoordinate, Colors.Purple, mapLayer);
                }
            }

            MyMap.Layers.Add(mapLayer);
        }

        private void DrawMapMarker(GeoCoordinate coordinate, Color color, MapLayer mapLayer)
        {
            // Create a map marker
            Polygon polygon = new Polygon();
            polygon.Points.Add(new Point(0, 0));
            polygon.Points.Add(new Point(0, 75));
            polygon.Points.Add(new Point(25, 0));
            polygon.Fill = new SolidColorBrush(color);

            // Enable marker to be tapped for location information
            polygon.Tag = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            polygon.MouseLeftButtonUp += new MouseButtonEventHandler(Marker_Click);

            // Create a MapOverlay and add marker.
            MapOverlay overlay = new MapOverlay();
            overlay.Content = polygon;
            overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            overlay.PositionOrigin = new Point(0.0, 1.0);
            mapLayer.Add(overlay);
        }

        private void DrawAccuracyRadius(MapLayer mapLayer)
        {
            // The ground resolution (in meters per pixel) varies depending on the level of detail 
            // and the latitude at which it’s measured. It can be calculated as follows:
            double metersPerPixels = (Math.Cos(MyCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = _accuracy / metersPerPixels;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0));

            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(MyCoordinate.Latitude, MyCoordinate.Longitude);
            overlay.PositionOrigin = new Point(0.5, 0.5);
            mapLayer.Add(overlay);
        }

        private async void GetCurrentCoordinate()
        {
            ShowProgressIndicator("Getting Location");
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;

            try
            {
                Geoposition currentPosition = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                _accuracy = currentPosition.Coordinate.Accuracy;

                Dispatcher.BeginInvoke(() =>
                {
                    MyCoordinate = new GeoCoordinate(currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude);
                    locationToBeShared = MyCoordinate;
                    DrawMapMarkers();
                    MyMap.SetView(MyCoordinate, 10, MapAnimationKind.Parabolic);
                    shareIconButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                // Couldn't get current location - location might be disabled in settings
                MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
            }
            HideProgressIndicator();
        }

        private void ShowProgressIndicator(String msg)
        {
            if (ProgressIndicator == null)
            {
                ProgressIndicator = new ProgressIndicator();
                ProgressIndicator.IsIndeterminate = true;
            }

            ProgressIndicator.Text = msg;
            ProgressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, ProgressIndicator);
        }

        private void HideProgressIndicator()
        {
            ProgressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, ProgressIndicator);
        }

        public ShareLocation()
        {
            InitializeComponent();
            GetCurrentCoordinate();
            BuildApplicationBar();
        }

        private void shareBtn_Click(object sender, EventArgs e)
        {
            byte[] thumbnailBytes = captureThumbnail();
            JObject metadata = new JObject();
            JArray filesData = new JArray();
            JObject singleFileInfo = new JObject();
            singleFileInfo[HikeConstants.FILE_NAME] = "Location";
            singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = "hikemap/location";
            singleFileInfo[HikeConstants.LATITUDE] = locationToBeShared.Latitude;
            singleFileInfo[HikeConstants.LONGITUDE] = locationToBeShared.Longitude;
            singleFileInfo[HikeConstants.ZOOM_LEVEL] = MyMap.ZoomLevel;
            singleFileInfo[HikeConstants.LOCATION_ADDRESS] = "";

            filesData.Add(singleFileInfo.ToObject<JToken>());

            metadata[HikeConstants.FILES_DATA] = filesData;

            object[] locationDetails = new object[2];
            locationDetails[0] = metadata;
            locationDetails[1] = thumbnailBytes;
            PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION] = locationDetails;
            MyMap = null;
            NavigationService.GoBack();
        }


        private void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Point p = e.GetPosition(this.MyMap);
            GeoCoordinate geo = new GeoCoordinate();
            geo = MyMap.ConvertViewportPointToGeoCoordinate(p);
            //map.ZoomLevel = 17;
            MyMap.Center = geo;
            locationToBeShared = geo;
            MyCoordinate = geo;
            DrawMapMarkers();
        }

        private byte[] captureThumbnail()
        {
            byte[] thumbnailBytes = null;
            WriteableBitmap screenshot = new WriteableBitmap(MyMap, new TranslateTransform());
            using (MemoryStream ms = new MemoryStream())
            {
                screenshot.SaveJpeg(ms, HikeConstants.LOCATION_THUMBNAIL_MAX_WIDTH, HikeConstants.LOCATION_THUMBNAIL_MAX_HEIGHT,
                    0, 47);
                thumbnailBytes = ms.ToArray();
            }
            return thumbnailBytes;
        }

        
    }
}