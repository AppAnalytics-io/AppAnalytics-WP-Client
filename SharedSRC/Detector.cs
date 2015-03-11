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
using Microsoft.Phone.Shell;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Globalization;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Navigation;
#endif


namespace AppAnalytics
{
    public static class API
    {
        /// <summary>
        /// Init API with your application key (Length == 32)
        /// </summary>
        /// <param name="aApiKey"></param>
        public static void init(string aApiKey)
        {
            Detector.init(aApiKey);
        }
        /// <summary>
        /// Register an event
        /// </summary>
        /// <param name="aDescription">Name/description of an event</param>
        public static void logEvent(String aDescription)
        {
            EventsManager.Instance.pushEvent(aDescription);
        }
        /// <summary>
        /// Register an event
        /// </summary>
        /// <param name="aDescription">Name/description of an event</param>
        /// <param name="aParameters">Additional parameters as key/value pair</param>
        public static void logEvent(String aDescription, Dictionary<string, string> aParameters)
        {
            EventsManager.Instance.pushEvent(aDescription, aParameters);
        }
        /// <summary>
        /// Enable or disable debug log output
        /// </summary>
        public static bool DebugLogEnabled
        {
            set { EventsManager.Instance.DebugLogEnabled = value; }
            get { return EventsManager.Instance.DebugLogEnabled; }
        }
        /// <summary>
        /// Enable or disable exception analytics
        /// </summary>
        public static bool ExceptionAnaluticsEnabled
        {
            set { EventsManager.Instance.ExceptionAnalyticsEnabled = value; }
            get { return EventsManager.Instance.ExceptionAnalyticsEnabled; }
        }
        /// <summary>
        /// Enable or disable exception analytics
        /// </summary>
        public static bool ScreenAnaluticsEnabled
        {
            set { EventsManager.Instance.ScreenAnalitycsEnabled = value; }
            get { return EventsManager.Instance.ScreenAnalitycsEnabled; }
        }
        /// <summary>
        /// Gets or sets event dispatch interval in seconds (max - 3600, min - 10, def - 120)
        /// </summary>
        public static float DispatchInterval
        {
            set { EventsManager.Instance.DispatchInterval = value; }
            get { return EventsManager.Instance.DispatchInterval; }
        }
        /// <summary>
        /// Enable or disable transactions analytics (currently unavailable)
        /// </summary>
        public static bool TransactionAnalyticsEnabled
        {
            set { EventsManager.Instance.TransactionAnaliticsEnabled = value; }
            get { return EventsManager.Instance.TransactionAnaliticsEnabled; }
        }
    }

    internal static class Detector
    {
        static readonly object _lockObject = new object();

        //members & properties /////////////////////////////////////////////////
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
        private static string mPreviousType = "";

        private static byte[] mApiKey = null;

        internal  static byte[] ApiKey
        {
            get { return mApiKey; }
        }
        // only needed on Silverlight
        internal static int UIThreadID
        {
            get
            {
                return mUIThreadID;
            }
        }

        #region resolution_getters
#if SILVERLIGHT
        internal static byte[] getResolutionX()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            //double h = (int)Math.Ceiling(content.ActualHeight * scale);
            double w = (int)Math.Ceiling(content.ActualWidth * scale);

            return BitConverter.GetBytes( w );
        }

        internal static byte[] getResolutionY()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            double h = (int)Math.Ceiling(content.ActualHeight * scale);
            //double w = (int)Math.Ceiling(content.ActualWidth * scale);

            return BitConverter.GetBytes( h );
        }
#else
        internal static byte[] getResolutionX()
        {
            return BitConverter.GetBytes(Math.Floor(ScreenSize.Width));
        }
        internal static byte[] getResolutionY()
        {
            return BitConverter.GetBytes(Math.Floor(ScreenSize.Height));
        }

        internal static double getResolutionXDouble()
        {
            return Math.Floor(ScreenSize.Width);
        }
        internal static double getResolutionYDouble()
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

        internal static byte ApiVersion = 1;

        //internal section //////////////////////////////////////////////////
        static internal byte[] getSessionID()
        {
            return mIDGen.SessionID;
        }
        static internal string getSessionIDString()
        {
            var raw = mIDGen.SessionIDRaw;

            return raw.ToString("N");
        }

        static internal string getSessionIDStringWithDashes()
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
            var pageType = (Window.Current.Content as Frame).Content.GetType();
            mPreviousType = pageType.Name;
#endif

            string nUri = uri.ToString();
            lock (_lockObject)
            {
                if (nUri != mPreviousUri && !mFirstLaunch)
                {
                    mNavigationOccured = true;
                    mPreviousUri = nUri;
                }
                else
                {
                    mFirstLaunch = false;
                    mPreviousUri = nUri;
                }
            }
        }


        static private void testException(object o)
        {
            throw new NullReferenceException("test exception");
        }

#if SILVERLIGHT
        static internal void exceptionsLogger(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();

            info.Add("Call stack", e.ExceptionObject.StackTrace);
            info.Add("Exception", e.ExceptionObject.ToString());
            info.Add("Type", e.ExceptionObject.GetType().Name);

            EventsManager.Instance.pushEvent("UnhandledException", info);
            EventsManager.Instance.store();
            Debug.WriteLine("Unhanded exception.");
        }

        static internal bool isInUITHread()
        {
            return mUIThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;
        }
        static void navigating(object sender, NavigatingCancelEventArgs e)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();
            string uri = "";
            lock (_lockObject)
            {
                uri = mPreviousUri;
            }

            if (uri != e.Uri.ToString())
            {
                var t = System.Enum.GetName(typeof(NavigationMode), e.NavigationMode);
                info.Add("Navigation Mode", t);
                info.Add("Destination URI", e.Uri.ToString());
                info.Add("Source URI", uri);

                EventsManager.Instance.pushEvent("Navigation", info);
                EventsManager.Instance.store();
            }
        }
        static async void initNavigationEvent()
        {
            PhoneApplicationFrame frame = null;
            while (null == frame)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                            frame = (Application.Current.RootVisual as PhoneApplicationFrame) );
                await Task.Delay(10);
            }
            frame.Navigating += navigating;
        }
#else
        static internal void exceptionsLogger(object sender, UnhandledExceptionEventArgs e)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();

            info.Add("Сall stack", e.Exception.StackTrace);
            info.Add("Exception", e.Exception.ToString());
            info.Add("Type", e.Exception.GetType().Name);

            EventsManager.Instance.pushEvent("UnhandledException", info);
            EventsManager.Instance.store();
            Debug.WriteLine("Unhanded exception.");
        }

        static void navigating(object sender, NavigationEventArgs e)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();

            string prev = "";
            lock (_lockObject)
            {
                prev = mPreviousType;
            }

            {
                var t = System.Enum.GetName(typeof(NavigationMode), e.NavigationMode);
                info.Add("Navigation Mode", t);
                info.Add("Destination Type", e.SourcePageType.Name);
                info.Add("Source Type", prev);

                EventsManager.Instance.pushEvent("Navigation", info);
            }
        }
#endif

        static internal void init(string aApiKey)
        {
            var current = Application.Current;
#if SILVERLIGHT
            mUIThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Recognizer.Instance.Init();
            current.UnhandledException += exceptionsLogger;

            var navigationTask = new Task(initNavigationEvent);
            navigationTask.Start();
            PhoneApplicationService.Current.Deactivated += onAppSuspend;
#else
            RTRecognizer.Instance.init();
            current.UnhandledException += exceptionsLogger; ;
            var view = Window.Current.Content as Frame;
            view.Navigated += navigating;
#endif
            EventsManager.Instance.init();
            CoreApplication.Exiting += onAppExit;
            CoreApplication.Suspending += onAppSuspend;

            // < TESTING. temporary

            //EventsManager.Instance.testSerialization();
            //EventsManager.Instance.testSerializationUsingMemoryStream()

            //EventsManager.Instance.testSending();
            EventsManager.Instance.DebugLogEnabled = true;
            EventsManager.Instance.DispatchInterval = 11;

            //var tmr = new Timer(testException, null, 10, Timeout.Infinite);
            // TESTING >

            var tsk = new Task(mIDGen.init);
            tsk.Start();
            tsk.Wait();

            mApiKey = Encoding.UTF8.GetBytes(aApiKey);
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

        static internal void terminate()
        {
            mKeepWorking = false;
        }

        static internal void pushReport(GestureData aData)
        {
            lock (_lockObject)
            {
                 ManifestController.Instance.buildDataPackage(aData);
            }
        }

        static DateTime mPrevTime = DateTime.Now;

        internal static void  handleTaps(double deltaT)
        {
#if SILVERLIGHT
            double tstDif = Recognizer.getNow() - Recognizer.Instance.PrevTapOccured;

            if ((tstDif > Recognizer.Instance.TimeForTap) && (Recognizer.Instance.TapsInRow > 0))
            {
                //Debug.WriteLine(Recognizer.Instance.TapsInRow + " < taps with > " + Recognizer.Instance.LastTapFingers);

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
                    //Debug.WriteLine("hit");
                    lock (_lockObject)
                    {
                        mNavigationOccured = false;
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
            double sec = 0;//Math.Floor(diff.TotalSeconds);

            return BitConverter.GetBytes(sec);
        }

        internal static byte[] getUDID()
        {
            return mIDGen.UDID;
        }

        internal static byte[] getUDID32()
        {
            var bts = Encoding.UTF8.GetBytes(mIDGen.UDIDRaw.ToString("N"));
            Debug.Assert(bts.Length == 32);
            return bts;
        }

        internal static string  getUDIDString()
        {
            var tst = mIDGen.UDIDRaw.ToString("N");
            var tst2 = mIDGen.UDIDRaw.ToString();
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

        internal static byte[] AppVersion
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
//         internal static byte[] getBytes(string str)
//         {
//             byte[] bytes = new byte[str.Length ];
//
//             for (int i = 0; i < str.Length; ++i )
//             {
//                 bytes[i] = (byte)str[i];
//             }
//
//             return bytes;
//         }

        private static byte[] toBytes(int val)
        {
            return BitConverter.GetBytes(val);
        }

        internal static double getCurrentDouble()
        {
            var date = DateTime.Now;

            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            double sec =  Math.Floor(diff.TotalSeconds);
            return sec;
        }

        internal static byte[] OSVersion
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

        internal static byte[] SystemLocale
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
                    GeographicRegion userRegion = new GeographicRegion();
                    lt3 = userRegion.CodeThreeLetter;
                }
                catch { }
#endif
                return new byte[3] { (byte)lt3[0], (byte)lt3[1], (byte)lt3[2] };
            }
        }

        static void onAppExit(object obj, object e)
        {
            storeAll();
        }

        static void onAppSuspend(object obj, object e)
        {
            storeAll();
        }

        static public void storeAll()
        {
            var tmp = new Task(ManifestController.Instance.store);
            tmp.Start();
            tmp.Wait();

            EventsManager.Instance.store();
        }

        #region debug_info_logging
        internal static void logSampleDbg(GestureData aGD)
        {
            if (EventsManager.Instance.DebugLogEnabled)
            {
                Debug.WriteLine("Order ID [{0}]", BitConverter.ToUInt64(aGD.ActionOrder,0) );
                Debug.WriteLine("Type [{0} to string -> {1}]", aGD.ActionID, aGD.typeToString() );
                Debug.WriteLine("Time [{0}]", aGD.mTimeObject.ToString());
                Debug.WriteLine("Position X|Y [{0}|{1}]", aGD.mPosX, aGD.mPosY);
                Debug.WriteLine("Param1  [{0}]", aGD.Param1asInt32);
                Debug.WriteLine("Page    [{0}]", Encoding.UTF8.GetString(aGD.ViewID, 0, aGD.ViewIDLenght) );
                Debug.WriteLine("Element [{0}]", Encoding.UTF8.GetString(aGD.ElementID, 0, aGD.ElementIDLenght));
                Debug.WriteLine(":::::::::::::::::::::::::::::::::");
            }
        }

        internal static void logEventDbg(AAEvent aEvent)
        {
            if (EventsManager.Instance.DebugLogEnabled)
            {
                string eventToStr = aEvent.ToString();
                Debug.WriteLine(eventToStr);
                Debug.WriteLine(":::::::::::::::::::::::::::::::::");
            }
        }
        #endregion

    }
}
