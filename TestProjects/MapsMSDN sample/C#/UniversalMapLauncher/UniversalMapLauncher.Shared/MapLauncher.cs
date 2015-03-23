using System;
using Windows.UI.Popups;

namespace UniversalMapLauncher
{
    public static class MapLauncher
    {
        #region Private Properties 

        private static string baseUri = "bingmaps:?";

        #endregion

        #region Public Methods

        public static void LaunchMap(double latitude, double longitude, int zoom)
        {
            string uri = string.Format("{0}cp={1:N5}~{2:N5}&lvl={3}", baseUri, latitude, longitude, zoom);

            Launch(new Uri(uri));
        }

        public static void LaunchMap(string query)
        {
            string uri = baseUri + "&q=" + Uri.EscapeDataString(query);

            Launch(new Uri(uri));
        }

        public static void LaunchMap(string start, string end)
        {
            string uri = string.Format("{0}rtp=adr.{1}~adr.{2}", baseUri, Uri.EscapeDataString(start), Uri.EscapeDataString(end));

            Launch(new Uri(uri));
        }

        #endregion

        #region Private Methods

        private static async void Launch(Uri uri)
        {
            // Launch the URI
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);

            if (!success)
            {
                //Failed to launch maps 
                var msg = new MessageDialog("Failed to launch maps app.");
                await msg.ShowAsync();
            }
        }

        #endregion
    }
}

