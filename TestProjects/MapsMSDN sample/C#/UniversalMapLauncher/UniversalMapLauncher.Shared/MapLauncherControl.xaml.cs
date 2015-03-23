using System;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniversalMapLauncher
{
    public sealed partial class MapLauncherControl : UserControl
    {
        #region Constructor

        public MapLauncherControl()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Button Handlers

        private void LaunchCenterZoom_Tapped(object sender, TappedRoutedEventArgs e)
        {
            double lat, lon;
            int zoom;

            if (!double.TryParse(LatitudeTbx.Text, out lat))
            {
                ShowMessage("Invalid latitude value.");
                return;
            }

            if (!double.TryParse(LongitudeTbx.Text, out lon))
            {
                ShowMessage("Invalid longitude value.");
                return;
            }

            if (!int.TryParse(ZoomTbx.Text, out zoom))
            {
                ShowMessage("Invalid zoom value.");
                return;
            }

            MapLauncher.LaunchMap(lat, lon, zoom);
        }

        private void LaunchWhatWhere_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(QueryTbx.Text))
            {
                ShowMessage("Invalid query value.");
                return;
            }

            MapLauncher.LaunchMap(QueryTbx.Text);
        }

        private void LaunchRoute_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartTbx.Text))
            {
                ShowMessage("Invalid start value.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EndTbx.Text))
            {
                ShowMessage("Invalid end value.");
                return;
            }
            
            MapLauncher.LaunchMap(StartTbx.Text, EndTbx.Text);
        }

        #endregion

        #region Private Methods

        private async void ShowMessage(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
        }

        #endregion
    }
}
