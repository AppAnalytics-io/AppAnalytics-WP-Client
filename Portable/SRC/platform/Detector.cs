using System;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
//using System.Windows.Navigation;
using System.Text;
using System.Threading;
using System.Windows.Input;
//using Microsoft.Phone.Controls;
//using Microsoft.Xna.Framework.Input.Touch;
using System.Diagnostics;
using System.Globalization;
using Windows.Graphics.Display;
using System.Collections;
using Windows.UI.Xaml;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using AppAnalitics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Geolocation;
using Windows.ApplicationModel.Core;
using System.Net.Http;
using System.Net.NetworkInformation;
using Windows.Globalization;
using Windows.UI.Input;
//using Microsoft.Phone.Net.NetworkInformation; 


namespace AppAnalytics
{
    public static class Detector
    {
        //static Windows.UI.Input.GestureRecognizer p1 = new Windows.UI.Input.GestureRecognizer();
        static readonly object _lockObject = new object();
        //gestures to-send queue 

        // Concurent.ConcurrentQueue<GestureData> mToSend = new ConcurrentQueue<GestureData>();
        //private static Queue<GestureData> mToSend = new Queue<GestureData>();
        
        //private section /////////////////////////////////////////////////
        private static UUID.UDIDGen mIDGen = UUID.UDIDGen.Instance;

        private static bool mKeepWorking = true;
        private static bool mNavigationOccured = false;

        private static Task mWorker = null;

        private static double mSessionStartTime = 0;

        private static string mPreviousUri = "";

        private static byte[] mApiKey = null;

        public  static byte[] ApiKey
        {
            get { return mApiKey; }
        }

        public static byte[] getResolutionX()
        {
            return BitConverter.GetBytes( Math.Floor( ScreenSize.Width) );
        }

        public static double getResolutionXDouble()
        {
            return Math.Floor(ScreenSize.Width);
        }
        public static double getResolutionYDouble()
        {
            return Math.Floor(ScreenSize.Height);
        }
        // actually it returns a Window size which is the same as screen size in WP apps or full screen apps
        static private Size ScreenSize
        {
            get
            {
                var bounds = Window.Current.Bounds;
                double w = bounds.Width;
                double h = bounds.Height;
                Size resolution = new Size();

                switch (DisplayInformation.GetForCurrentView().ResolutionScale)
                {
                    case ResolutionScale.Scale120Percent:
                        w = Math.Ceiling(w * 1.2);
                        h = Math.Ceiling(h * 1.2);

                        break;
                    case ResolutionScale.Scale150Percent:
                        w = Math.Ceiling(w * 1.5);
                        h = Math.Ceiling(h * 1.5);

                        break;
                    case ResolutionScale.Scale160Percent:
                        w = Math.Ceiling(w * 1.6);
                        h = Math.Ceiling(h * 1.6);

                        break;
                    case ResolutionScale.Scale225Percent:
                        w = Math.Ceiling(w * 2.25);
                        h = Math.Ceiling(h * 2.25);

                        break;
                    case ResolutionScale.Scale140Percent:
                        w = Math.Ceiling(w * 1.4);
                        h = Math.Ceiling(h * 1.4);

                        break;
                    case ResolutionScale.Scale180Percent:
                        w = Math.Ceiling(w * 1.8);
                        h = Math.Ceiling(h * 1.8);

                        break;
                }

                if (ApplicationView.GetForCurrentView().IsFullScreen
                    && ApplicationView.GetForCurrentView().Orientation == ApplicationViewOrientation.Landscape)
                {
                    resolution = new Size(w, h);
                }
                else if (ApplicationView.GetForCurrentView().IsFullScreen
                    && ApplicationView.GetForCurrentView().Orientation == ApplicationViewOrientation.Landscape)
                {
                    resolution = new Size(h, w);
                }
                else
                {
                    resolution = new Size(w + 320.0 + 22.0, h);
                }
                return resolution;
            }
        }

        public static byte[] getResolutionY()
        {
            
            return BitConverter.GetBytes( Math.Floor(ScreenSize.Height) );
        }
        public static byte ApiVersion = 1;

        //public section //////////////////////////////////////////////////
        static public byte[] getSessionID()
        {
            return mIDGen.SessionID;
        }
        static public string getSessionIDString()
        {
            return Encoding.UTF8.GetString(mIDGen.SessionID, 0 , mIDGen.SessionID.Length);
        }

        static private bool mFirstLaunch = true;
        static private void getCurent()
        {
            var currentPage = Window.Current.Content as Frame;
            //Frame.

            if (null != currentPage) return;

            var uri = currentPage.BaseUri;

            string nUri = uri.ToString();
            if (nUri != mPreviousUri && !mFirstLaunch)
            {
                lock (_lockObject)
                {
                    mNavigationOccured = true;
                    mPreviousUri = nUri;
                }
            }
            else
            {
                mFirstLaunch = false;
                mPreviousUri = nUri;
            }
        }

        static public void init(string aApiKey)
        {
            //TODO : RENEW
            //Recognizer.Instance.Init();
            RTRecognizer.Instance.init();

            mApiKey = getBytes(aApiKey);
            if (mApiKey.Length != 32)
            {
                Debug.WriteLine("API key length is not equal 32");
                mApiKey = new byte[32];
                mApiKey.Initialize();
            }

            if (null == mWorker)
            {

                var date = DateTime.Now;

                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                TimeSpan diff = date.ToUniversalTime() - origin;
                mSessionStartTime = Math.Floor(diff.TotalSeconds);
                sendManifest();

                mWorker = new Task(updateLoop);
                //mWorker.IsBackground = true;
                mWorker.Start();
            }
        }

        static void sendManifest()
        {
            var it = ManifestController.Instance;
            it.buildSessionManifest();
            it.sendManifest();
        }

        static public void terminate()
        {
            mKeepWorking = false;
        }

        static public void pushReport(GestureData aData)
        {
            lock (_lockObject)
            {
                 ManifestController.Instance.buildDataPackage(aData);
            }
        }

        static DateTime mPrevTime = DateTime.Now;

        public static void  handleTaps(double deltaT)
        {
            //TODO : RENEW
            /*
            double tstDif = Recognizer.getNow() - Recognizer.Instance.PrevTapOccured;
            
            if ((tstDif > Recognizer.Instance.TimeForTap) && (Recognizer.Instance.TapsInRow > 0))
            {
                Debug.WriteLine(Recognizer.Instance.TapsInRow + " < taps with > " + Recognizer.Instance.LastTapFingers);

                Recognizer.Instance.createTapGesture(Recognizer.Instance.TapsInRow, Recognizer.Instance.LastTapFingers);
                Recognizer.Instance.TapsInRow = 0;
            }*/
        }

        static double toSendMark = 0;
        static double toStoreMark = 0;
        const double kSendConst = 20;
        const double kStoreConst = 15;

        static async private void updateLoop()
        {
            GestureRecognizer rc = new GestureRecognizer();
            
            while (mKeepWorking)
            {
                var date = DateTime.Now;

                TimeSpan diff = date.ToUniversalTime() - mPrevTime.ToUniversalTime();
                double sec = diff.TotalSeconds;
                mPrevTime = date;

                toSendMark += sec;
                toStoreMark += sec;

                if (toStoreMark > kStoreConst)
                {
                    ManifestController.Instance.store();
                    toStoreMark = 0;
                }
                if (toSendMark > kSendConst)
                {
                    ManifestController.Instance.sendSamples();
                    toSendMark = 0;
                }

                handleTaps(sec);
                try
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () => getCurent());
                }
                catch
                {
                    Debug.WriteLine("Detector: unable to get current view for now.");
                }
                if (mNavigationOccured)
                {
                    lock (_lockObject)
                    {
                        mNavigationOccured = false;
                        //Recognizer.Instance.createGesture(GestureID.Navigation);
                    }
                }

                //var gestres = TouchPanel.GetState();
                #region processing
                ////                 while (TouchPanel.IsGestureAvailable)
                ////                 { 
                ////                     Deployment.Current.Dispatcher.BeginInvoke(() => getCurent()); 
                //// 
                ////                     GestureSample gs = TouchPanel.ReadGesture();
                ////                     //TouchPanel.GetState().
                ////                     switch (gs.GestureType)
                ////                     {
                ////                         case GestureType.VerticalDrag:
                ////                             verticalDragStarted = true;
                ////                             
                ////                             Debug.WriteLine("   +vertical drag catched\n");
                //// 
                ////                             break;
                //// 
                ////                         case GestureType.Flick:
                ////                             Debug.WriteLine("   +flick catched\n");
                ////                             var da = Math.Abs(gs.Delta.X) + Math.Abs(gs.Delta.Y);
                ////                             var dn = Math.Abs(gs.Delta2.X) + Math.Abs(gs.Delta2.Y);
                //// 
                ////                             if (da > 0 && dn > 0 )
                ////                             {
                ////                                 Debug.WriteLine("   +flick2x catched\n");
                ////                             }
                //// 
                ////                             break;
                //// 
                ////                         case GestureType.Tap:
                ////                             Debug.WriteLine("   +tap cathced\n");
                ////                                                         
                ////                             break;
                //// 
                ////                         case GestureType.PinchComplete:
                ////                             Debug.WriteLine("   +pinch catched\n");
                ////                             
                ////                             break;
                //// 
                ////                         case GestureType.HorizontalDrag:
                ////                             horizontalDragStarted = true;
                ////                             Debug.WriteLine("   +horizontal drag catched\n");
                //// 
                ////                             break;
                //// 
                ////                         case GestureType.Hold:
                ////                             Debug.WriteLine("   +hold catched\n");
                //// 
                ////                             break;
                //// 
                ////                         case GestureType.DragComplete:
                ////                             if(horizontalDragStarted)
                ////                             {
                ////                                 Debug.WriteLine("<- horiz. ended\n");
                ////                             }
                ////                             if(verticalDragStarted)
                ////                             {
                ////                                 Debug.WriteLine("<- vert. ended\n");
                ////                             }
                ////                             horizontalDragStarted = false;
                ////                             verticalDragStarted = false;
                ////                             Debug.WriteLine("   +hold catched\n");
                //// 
                ////                             break;
                ////                     }
                ////                 } of while
                #endregion
            }
        }

        internal static byte[] getSessionStartDate()
        {
            return BitConverter.GetBytes(mSessionStartTime);
        }

        internal static byte[] getSessionEndDate()
        {
            double sec = 0;

            return BitConverter.GetBytes(sec);
        }

        internal static byte[] getUDID()
        {
            return mIDGen.UDID;
        }

        internal static string  getUDIDString()
        {
            return Encoding.UTF8.GetString(mIDGen.UDID, 0, mIDGen.UDID.Length);
        }

        private static string getAppVersion()
        {
            var xmlReaderSettings = new XmlReaderSettings();

            try
            {
                PackageVersion vs = Windows.ApplicationModel.Package.Current.Id.Version;
                return vs.Build + "." + vs.Major + "." + vs.Minor + "." + vs.Revision;
            }
            catch { Debug.WriteLine("prob. null ref expn"); }
            return "";
        }

        public static byte[] AppVersion 
        {
            get 
            {
                byte[] bts = new byte[16];
                bts.Initialize();

                string[] v4 = getAppVersion().Split('.');

                if (v4.Length != 4) 
                {
                    return bts; //16*0
                }

                int i = 0;
                foreach (var v in v4)
                {
                    //bts[i] = (byte) v[0];
                    var parsed = BitConverter.GetBytes(UInt32.Parse(v));
                    System.Buffer.BlockCopy( parsed, 0, bts, i*4, parsed.Length);
                    ++i;
                }

                return bts;
            }
        }

        // converting char for 2bytes approach to 1byte
        public static byte[] getBytes(string str)
        {
            byte[] bytes = new byte[str.Length ];

            for (int i = 0; i < str.Length; ++i )
            {
                bytes[i] = (byte)str[i];
            }

            return bytes;
        }

        private static byte[] toBytes(int val)
        {
            return BitConverter.GetBytes(val);
        }

        public static byte[] OSVersion 
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                var vs = Utils.OSVersion; // TODO: implement that for universal apps. upd: there is no way to di it.

                byte[] buffer = new byte[16]; 
                System.Buffer.BlockCopy(toBytes(vs.Major), 0, buffer, 0, toBytes(vs.Major).Length);
                System.Buffer.BlockCopy(toBytes(vs.Minor), 0, buffer, 4, toBytes(vs.Minor).Length);
                System.Buffer.BlockCopy(toBytes(vs.Build), 0, buffer, 8, toBytes(vs.Build).Length);
                System.Buffer.BlockCopy(toBytes(vs.Revision), 0, buffer, 12, toBytes(vs.Revision).Length);

                return buffer;  
            }
        }

        public static byte[] SystemLocale 
        {
            get 
            {
                string lt3 = "USA";
                // potential issues
                try
                {
                    // this statement may probably generate wrong data, 
                    // at least it is not as stable as silverlight version
                    GeographicRegion userRegion = new GeographicRegion();
                    lt3 = userRegion.CodeThreeLetter; 
                }
                catch { }
                
                return new byte[3] { (byte)lt3[0], (byte)lt3[1], (byte)lt3[2] };
            }
        }

        // rewrite trying to get location, otherwise - use current region
//         static async void tryToGetPos()
//         {
//             Geolocator g = new Geolocator();
//             var pos = await g.GetGeopositionAsync(TimeSpan.MaxValue, new TimeSpan(100000));
// 
//             var addr = pos.CivicAddress;
//             gpos = pos;
//         }
    }


}
