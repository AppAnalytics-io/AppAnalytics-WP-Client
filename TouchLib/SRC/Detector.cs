using System;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
using System.Windows.Navigation;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Input.Touch;
using System.Diagnostics;
using System.Globalization;
using Windows.Graphics.Display;
using System.Collections;
using Microsoft.Phone.Net.NetworkInformation; 


namespace TouchLib
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

        private static Thread mWorker = null;

        private static double mSessionStartTime = 0;

        private static string mPreviousUri = "";

        private static byte[] mApiKey = null;

        public  static byte[] ApiKey
        {
            get { return mApiKey; }
        }

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
            var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;

            var uri = currentPage.NavigationService.CurrentSource;

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
            Recognizer.Instance.Init();
            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(changeDetected);

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

                mWorker = new Thread(updateLoop);
                mWorker.IsBackground = true;
                mWorker.Start();
            }
        }

        static void changeDetected(object sender, NetworkNotificationEventArgs e)
        {
            string change = string.Empty;
            switch (e.NotificationType)
            {
                case NetworkNotificationType.InterfaceConnected:
                    change = "Connected to ";
                    break;
                case NetworkNotificationType.InterfaceDisconnected:
                    change = "Disconnected from ";
                    break;
                case NetworkNotificationType.CharacteristicUpdate:
                    change = "Characteristics changed for ";
                    break;
                default:
                    change = "Unknown change with ";
                    break;
            }

            string changeInformation = String.Format(" {0} {1} {2} ({3})",
                        DateTime.Now.ToString(), change, e.NetworkInterface.InterfaceName,
                        e.NetworkInterface.InterfaceType.ToString());

            Debug.WriteLine(changeInformation);

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
            double tstDif = Recognizer.getNow() - Recognizer.Instance.PrevTapOccured;
            
            if ((tstDif > Recognizer.Instance.TimeForTap) && (Recognizer.Instance.TapsInRow > 0))
            {
                Debug.WriteLine(Recognizer.Instance.TapsInRow + " < taps with > " + Recognizer.Instance.LastTapFingers);

                Recognizer.Instance.createTapGesture(Recognizer.Instance.TapsInRow, Recognizer.Instance.LastTapFingers);
                Recognizer.Instance.TapsInRow = 0;
            }
        }
        static double toSendMark = 0;
        static double toStoreMark = 0;
        const double kSendConst = 60;
        const double kStoreConst = 15;

        static private void updateLoop()
        {
            while (mKeepWorking)
            {
                var date = DateTime.Now;

                TimeSpan diff = date.ToUniversalTime() - mPrevTime;
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
                Deployment.Current.Dispatcher.BeginInvoke(() => getCurent());

                if (mNavigationOccured)
                {
                    lock (_lockObject)
                    {
                        mNavigationOccured = false;
                        Recognizer.Instance.createGesture(GestureID.Navigation);
                    }
                }

                var gestres = TouchPanel.GetState();
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
            return Encoding.UTF8.GetString(mIDGen.UDID, 0, mIDGen.UDID.Length);
        }

        private static string getAppVersion()
        {
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
                var vs = Environment.OSVersion.Version;

                byte[] buffer = new byte[16];
                System.Buffer.BlockCopy(toBytes(vs.Major), 0, buffer, 0, toBytes(vs.Major).Length);
                System.Buffer.BlockCopy(toBytes(vs.Minor), 0, buffer, 4, toBytes(vs.Minor).Length);
                System.Buffer.BlockCopy(toBytes(vs.Build), 0, buffer, 8, toBytes(vs.Build).Length);
                System.Buffer.BlockCopy(toBytes(vs.Revision), 0, buffer, 12, toBytes(vs.Revision).Length);

//                 sb.Append(getString(toBytes(vs.Major)))
//                   .Append(getString(toBytes(vs.Minor)))
//                   .Append(getString(toBytes(vs.Build)))
//                   .Append(getString(toBytes(vs.Revision)));

                return buffer;  
            }
        }

        public static byte[] SystemLocale 
        {
            get 
            {
                CultureInfo cult = Thread.CurrentThread.CurrentCulture;
                RegionInfo rf = new RegionInfo(cult.ToString());

                string lt3 = Converter.convertToThreeLetterCode( rf.TwoLetterISORegionName );

                return new byte[3] { (byte)lt3[0], (byte)lt3[1], (byte)lt3[2] };
            }
        }
    }
}
