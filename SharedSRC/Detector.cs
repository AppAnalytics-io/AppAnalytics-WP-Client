using System;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Diagnostics;
using System.Globalization;
using Windows.Graphics.Display;
using System.Collections;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
#if SILVERLIGHT
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Input.Touch;
using System.Windows.Navigation;
using Windows.ApplicationModel;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Globalization;
using Windows.ApplicationModel;
#endif


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
        private static int mUIThreadID = -1;
        private static bool mKeepWorking = true;
        private static bool mNavigationOccured = false;
#if SILVERLIGHT
        private static Thread mWorker = null;
#else
        private static Task mWorker = null;
#endif

        private static double mSessionStartTime = 0;

        private static string mPreviousUri = "";

        private static byte[] mApiKey = null;

        public  static byte[] ApiKey
        {
            get { return mApiKey; }
        }
        // only needed on Silverlight
        public static int UIThreadID
        {
            get
            {
                return mUIThreadID;
            }
        }
        #region resolution_getters
#if SILVERLIGHT
        public static byte[] getResolutionX()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            //double h = (int)Math.Ceiling(content.ActualHeight * scale);
            double w = (int)Math.Ceiling(content.ActualWidth * scale);
            
            return BitConverter.GetBytes( w );
        }

        public static byte[] getResolutionY()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            double h = (int)Math.Ceiling(content.ActualHeight * scale);
            //double w = (int)Math.Ceiling(content.ActualWidth * scale);

            return BitConverter.GetBytes( h );
        }
#else
        public static byte[] getResolutionX()
        {
            return BitConverter.GetBytes(Math.Floor(ScreenSize.Width));
        }
        public static byte[] getResolutionY()
        {
            return BitConverter.GetBytes(Math.Floor(ScreenSize.Height));
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
#endif
        #endregion

        public static byte ApiVersion = 1;

        //public section //////////////////////////////////////////////////
        static public byte[] getSessionID()
        {
            return mIDGen.SessionID;
        }
        static public string getSessionIDString()
        {
            var raw = mIDGen.SessionIDRaw;

            return raw.ToString("N");
        }

        static public string getSessionIDStringWithDashes()
        {
            return mIDGen.SessionIDRaw.ToString();
        }

        static private bool mFirstLaunch = true;
        static private void getCurent()
        { 
#if SILVERLIGHT
            var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;
            var uri = currentPage.NavigationService.CurrentSource;
#else
            var currentPage = Window.Current.Content as Frame;
            //Frame.
            if (null != currentPage) return;
            var uri = currentPage.BaseUri;
#endif
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
        
#if SILVERLIGHT
        static public bool isInUITHread()
        {
            return mUIThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;
        }
#endif
        static public void init(string aApiKey)
        {
#if SILVERLIGHT
            mUIThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Recognizer.Instance.Init();
#else
            RTRecognizer.Instance.init();
#endif

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
#if SILVERLIGHT
                mWorker = new Thread(updateLoop);
                mWorker.IsBackground = true;
#else
                mWorker = new Task(updateLoop);
#endif
                mWorker.Start();
            }
        }

        static void sendManifest()
        {
            ManifestController.Instance.buildSessionManifest();
            ManifestController.Instance.sendManifest();
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
#if SILVERLIGHT
            double tstDif = Recognizer.getNow() - Recognizer.Instance.PrevTapOccured;
            
            if ((tstDif > Recognizer.Instance.TimeForTap) && (Recognizer.Instance.TapsInRow > 0))
            {
                Debug.WriteLine(Recognizer.Instance.TapsInRow + " < taps with > " + Recognizer.Instance.LastTapFingers);

                Recognizer.Instance.createTapGesture(Recognizer.Instance.TapsInRow, Recognizer.Instance.LastTapFingers);
                Recognizer.Instance.TapsInRow = 0;
            }
#else
            double tstDif = (FrameProcessor.getNow() - FrameProcessor.Instance.PrevTapOccured).TotalSeconds;

            if ((tstDif > FrameProcessor.Instance.TimeForTap) && (FrameProcessor.Instance.TapsInRow > 0))
            {
                Debug.WriteLine(FrameProcessor.Instance.TapsInRow + " < taps with > " + FrameProcessor.Instance.LastTapFingers);

                GestureProcessor.createTapGesture(FrameProcessor.Instance.TapsInRow, FrameProcessor.Instance.LastTapFingers);
                FrameProcessor.Instance.TapsInRow = 0;
            }
#endif
        }
        static double toSendMark = 0;
        static double toStoreMark = 0;
        const double kSendConst = 20;
        const double kStoreConst = 15;

#if SILVERLIGHT
        static private void updateLoop()
#else
        static async private void updateLoop()
#endif
        {
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
                // test it more
#if SILVERLIGHT
                Deployment.Current.Dispatcher.BeginInvoke(() => getCurent());
#else
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
#endif
                if (mNavigationOccured)
                {
                    Debug.WriteLine("hit");
                    lock (_lockObject)
                    {
                        mNavigationOccured = false;
                        //todo
                        //Recognizer.Instance.createGesture(GestureID.Navigation);
                    }
                }
            }
        }

        internal static byte[] getSessionStartDate()
        {
            return BitConverter.GetBytes(mSessionStartTime);
        }

        internal static byte[] getSessionEndDate()
        {
//             var date = DateTime.Now;
// 
//             DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
//             TimeSpan diff = date.ToUniversalTime() - origin;
            double sec = 0;//Math.Floor(diff.TotalSeconds);

            return BitConverter.GetBytes(sec);
        }

        internal static byte[] getUDID()
        {
            return mIDGen.UDID;
        }

        internal static string  getUDIDString()
        {
            return mIDGen.UDIDRaw.ToString("N");
        }

        private static string getAppVersion()
        {
#if SILVERLIGHT
            var xmlReaderSettings = new XmlReaderSettings
            {
                XmlResolver = new XmlXapResolver()
            };

            using (var xmlReader = XmlReader.Create("WMAppManifest.xml", xmlReaderSettings))
            {
                if (null != xmlReader)
                {
                    xmlReader.ReadToDescendant("App");

                    return xmlReader.GetAttribute("Version");
                }
                else
                {
                    return "";
                }
            } 
#else
            try
            {
                PackageVersion vs = Windows.ApplicationModel.Package.Current.Id.Version;
                return vs.Build + "." + vs.Major + "." + vs.Minor + "." + vs.Revision;
            }
            catch { Debug.WriteLine("prob. null ref expn"); }
            return "";
#endif
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
#if SILVERLIGHT
                var vs = Environment.OSVersion.Version;
#else
                var vs = AppAnalytics.Utils.OSVersion; // TODO: implement that for universal apps. upd: there is no way to di it.
#endif

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
#if SILVERLIGHT
                CultureInfo cult = Thread.CurrentThread.CurrentCulture;
                RegionInfo rf = new RegionInfo(cult.ToString());   
                lt3 = Converter.convertToThreeLetterCode( rf.TwoLetterISORegionName );
#else
                try
                {
                    // this statement may probably generate wrong data, 
                    // at least it is not as stable as silverlight version
                    GeographicRegion userRegion = new GeographicRegion();
                    lt3 = userRegion.CodeThreeLetter; 
                }
                catch { }
#endif          
                return new byte[3] { (byte)lt3[0], (byte)lt3[1], (byte)lt3[2] };
            }
        }
    }
}
