﻿using System;
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
using System.Reflection; 
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Globalization;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;
using System.Reflection;
#endif

namespace AppAnalytics
{
    internal static class Detector
    {

        #region members_and_props
        static bool mIsInitialized = false;

        static readonly object _lockObject = new object();

        private static IManifestController mManifestSamplesController = null;

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

        private static byte[] mApiKey = new byte[32];

        public static bool IsInitialized
        {
            get { return mIsInitialized; }
        }

        internal  static byte[] ApiKey
        {
            get { return mApiKey; }
        }
        #endregion

        // only needed on Silverlight
        internal static int UIThreadID
        {
            get
            {  return mUIThreadID; }
        }

        public static IManifestController getManifestController()
        {
            return mManifestSamplesController;
        }

        #region resolution_getters
        internal static byte[] getResolutionX()
        { 
            return BitConverter.GetBytes(mResolutionX);
        }

        internal static byte[] getResolutionY()
        {
            return BitConverter.GetBytes(mResolutionY);
        }
#if SILVERLIGHT
        internal static void setResolutionX()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            //double h = (int)Math.Ceiling(content.ActualHeight * scale);
            mResolutionX = (int)Math.Ceiling(content.ActualWidth * scale); 
        }

        internal static void setResolutionY()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            mResolutionY = (int)Math.Ceiling(content.ActualHeight * scale); 
        }
#else
        internal static void setResolutionX()
        { 
            mResolutionX =  Math.Floor(ScreenSize.Width); 
        }

        internal static void setResolutionY()
        {
            mResolutionY = Math.Floor(ScreenSize.Height); 
        } 

        internal static double getResolutionXDouble()
        {
            return Math.Floor(mResolutionX);
        }
        internal static double getResolutionYDouble()
        {
            return Math.Floor(mResolutionY);
        }
        // actually it returns a Window size which is the same as screen size in WP apps or full screen apps
        static private Size ScreenSize
        {
            get
            {
                Size resolution = new Size();
                double w = 0;
                double h = 0; 

                Rect bounds = new Rect();
                //Window.Current is null under unit test project. beware
                bounds = Window.Current.Bounds;

                w = bounds.Width;
                h = bounds.Height;

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

                resolution = new Size(h, w); 
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
#if SILVERLIGHT
                    Recognizer.Instance.createGesture(GestureID.Navigation);
#else
                    GestureProcessor.createGesture(GestureID.Navigation);
#endif
                }
                else
                {
                    mFirstLaunch = false;
                    mPreviousUri = nUri;
                }
            }
        } 

#if SILVERLIGHT
        static internal void exceptionsLogger(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();
        
            info.Add(Defaults.ExceptionTxt.kStrCallStack, e.ExceptionObject.StackTrace);
            info.Add(Defaults.ExceptionTxt.kStrReason, e.ExceptionObject.ToString());
            info.Add(Defaults.ExceptionTxt.kStrType, e.ExceptionObject.GetType().Name); 
            EventsManager.Instance.pushEvent(Defaults.ExceptionTxt.kStrEventName, info);
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
                info.Add(Defaults.NavigationTxt.kStrType, t);
//                 info.Add("Destination URI", e.Uri.ToString());
//                 info.Add("Source URI", uri);

                EventsManager.Instance.pushEvent(Defaults.NavigationTxt.kStrEventName, info); 
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

            info.Add(Defaults.ExceptionTxt.kStrCallStack, e.Exception.StackTrace);
            info.Add(Defaults.ExceptionTxt.kStrReason, e.Exception.ToString());
            info.Add(Defaults.ExceptionTxt.kStrType, e.Exception.GetType().Name);

            EventsManager.Instance.pushEvent(Defaults.ExceptionTxt.kStrEventName, info);
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
                //var t = System.Enum.GetName(typeof(NavigationMode), e.NavigationMode); 
                info.Add(Defaults.NavigationTxt.kStrType, e.SourcePageType.Name);

                EventsManager.Instance.pushEvent(Defaults.NavigationTxt.kStrEventName, info);
            }
        }
#endif
        static internal void setupControllers(IManifestController aMC)
        {
            if (null == aMC)
            {
                throw new ArgumentNullException();
            }

            mManifestSamplesController = aMC; 
        }

        // I do realize that testing = false is too damn bad but for now 
        // let it be this way. will be changed soon
        static internal void init(string aApiKey, bool testing = false)
        {
            mApiKey = Encoding.UTF8.GetBytes(aApiKey);
            if (mApiKey.Length != 32)
            {
                throw new ArgumentOutOfRangeException("API key length is not equal 32");
            }

            if (mIsInitialized)
            {
                return;
            }
            else
            {
                mIsInitialized = true;
            }

            var current = Application.Current;
            if (!testing) // in unit tests Window.Current is null. 
                //this way we will skip this part
            {
                setResolutionX();
                setResolutionY();
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

                var tsk = new Task(mIDGen.init);
                tsk.Start();
                tsk.Wait();
            }

            sendManifest();
            if (mManifestSamplesController.SamplesCount > 0)
            {
                mManifestSamplesController.sendSamples();
            }

            if (null == mWorker && !testing)
            {
                var date = DateTime.Now;

                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                TimeSpan diff = date.ToUniversalTime() - origin;
                mSessionStartTime = Math.Floor(diff.TotalSeconds);

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
            CallSequenceMonitor.logCall();
            mManifestSamplesController.buildSessionManifest();
            mManifestSamplesController.sendManifest();
        }

        static internal void terminate()
        {
            mKeepWorking = false;
        }

        static internal void pushReport(GestureData aData)
        {
            lock (_lockObject)
            {
                mManifestSamplesController.buildDataPackage(aData);
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
                //Debug.WriteLine(FrameProcessor.Instance.TapsInRow + " < taps with > " + FrameProcessor.Instance.LastTapFingers); 
                GestureProcessor.createTapGesture(FrameProcessor.Instance.TapsInRow, FrameProcessor.Instance.LastTapFingers);
                FrameProcessor.Instance.TapsInRow = 0;
            }
#endif
        }
        static double          toSendMark = 0;
        static double          toStoreMark = 0;
        public const double    kSendConst = 60;
        public const double    kStoreConst = 15;

        public const double    kInsertMarkMsc = 150;
        static Stopwatch mInsertinonTimer = new Stopwatch();
        private static double mResolutionY = 0;
        private static double mResolutionX = 0;
   
#if SILVERLIGHT      
        static private void updateLoop() 
#else
        static private async void updateLoop() 
#endif
        {
            mInsertinonTimer.Start();
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
                    mManifestSamplesController.store();
                    toStoreMark = 0;
                }
                if (toSendMark > kSendConst)
                {
                    mManifestSamplesController.sendSamples();
                    toSendMark = 0;
                }
                if (mInsertinonTimer.ElapsedMilliseconds > kInsertMarkMsc)
                {
                    mInsertinonTimer.Restart();
                    EventsManager.Instance.insertEvents();
                }

                handleTaps(sec); 
#if SILVERLIGHT
                Deployment.Current.Dispatcher.BeginInvoke(() => getCurent());
#else
                try
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () => getCurent());
                }
                catch (Exception)
                {
                    Debug.WriteLine("Detector: unable to get current view for now.");
                }
#endif
                if (mNavigationOccured)
                { 
                    lock (_lockObject)
                    {
                        mNavigationOccured = false;
#if SILVERLIGHT
                        Recognizer.Instance.createGesture(GestureID.Navigation);
#else
                        GestureProcessor.createGesture(GestureID.Navigation);
#endif
                    }
                }
#if !SILVERLIGHT
                await Task.Delay(50);
#endif
            }
        }

        internal static byte[] getSessionStartDate()
        {
            return BitConverter.GetBytes(mSessionStartTime);
        }
        // unused
        internal static byte[] getSessionEndDate()
        {
            double sec = 0; 

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
            catch (Exception) { Debug.WriteLine("prob. null ref expn"); }
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
                    var parsed = BitConverter.GetBytes(UInt32.Parse(v));
                    System.Buffer.BlockCopy( parsed, 0, bts, i*4, parsed.Length);
                    ++i;
                }

                return bts;
            }
        } 

        private static byte[] toBytes(int val)
        {
            return BitConverter.GetBytes(val);
        }

        internal static double getCurrentTimeAsDouble()
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
                GeographicRegion userRegion = new GeographicRegion();
                lt3 = userRegion.CodeThreeLetter; 
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
            CallSequenceMonitor.logCall();
            var tmp = new Task(mManifestSamplesController.store);
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
