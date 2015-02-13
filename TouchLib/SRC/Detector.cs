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


namespace TouchLib
{
    public static class Detector
    {
        static Windows.UI.Input.GestureRecognizer p1 = new Windows.UI.Input.GestureRecognizer();
        //gestures to-send queue 

        // Concurent.ConcurrentQueue<GestureData> mToSend = new ConcurrentQueue<GestureData>();
        private static Queue<GestureData> mToSend = new Queue<GestureData>();
        
        //private section /////////////////////////////////////////////////
        private static UUID.UDIDGen mIDGen = UUID.UDIDGen.Instance;

        private static bool mKeepWorking = true;
        private static bool mNavigationOccured = false;

        private static Thread mWorker = null;
        private static Sender mSender = new Sender();
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

            return BitConverter.GetBytes(h);
        }
        public static byte ApiVersion = 1;

        //public section //////////////////////////////////////////////////
        static public byte[] getSessionID()
        {
            return mIDGen.SessionID;
        }

        static private void getCurent()
        { 
            var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;

            var uri = currentPage.NavigationService.CurrentSource;

            string nUri = uri.ToString();
            if (nUri != mPreviousUri)
            {
                lock (mPreviousUri)
                {
                    mNavigationOccured = true;
                }
            }
        }

        static public void init(string aApiKey)
        {
            Recognizer.Instance.Init();

            mApiKey = getBytes(aApiKey);
            if (mApiKey.Length != 32)
            {
                Debug.WriteLine("API key length is not equal 32");
                mApiKey = new byte[32];
                mApiKey.Initialize();
            }

            //mPreviousUri = getCurent();

            if (null == mWorker)
            {
//                 TouchPanel.EnabledGestures = GestureType.VerticalDrag | GestureType.HorizontalDrag | GestureType.Flick
//                     | GestureType.PinchComplete | GestureType.Pinch
//                     | GestureType.Hold | GestureType.Tap | GestureType.DoubleTap;

                mWorker = new Thread(updateLoop);
                mWorker.IsBackground = true;
                mWorker.Start();

                var date = DateTime.Now;

                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                TimeSpan diff = date.ToUniversalTime() - origin;
                mSessionStartTime = Math.Floor(diff.TotalSeconds);
            }
        }

        static public void terminate()
        {
            mKeepWorking = false;
        }

        static private string whereGestureHapend()
        {
            return "";
        }

        static private void pushReport(GestureData aData)
        {
            lock (mToSend)
            {
                mToSend.Enqueue (aData);
            }
        }

        static DateTime mPrevTime = DateTime.Now;

        public static void  handleTaps(double deltaT)
        {
            double tstDif = Recognizer.getNow() - Recognizer.Instance.PrevTapOccured;
            // TODO: fix hardcode
            if ((tstDif > 0.2f) && (Recognizer.Instance.TapsInRow > 0))
            {
                Debug.WriteLine(Recognizer.Instance.TapsInRow + " <taps with " + Recognizer.Instance.PrevFingers);
                Recognizer.Instance.TapsInRow = 0;
            }
        }

        static private void updateLoop()
        {
            while (mKeepWorking)
            {
                var date = DateTime.Now;

                TimeSpan diff = date.ToUniversalTime() - mPrevTime;
                double sec = Math.Floor(diff.TotalSeconds);
                mPrevTime = date;

                handleTaps(sec);

                if (mNavigationOccured)
                {
                    lock (mPreviousUri)
                    {
                        mNavigationOccured = false;
                        //create and gesture report
                    }
                }

                var gestres = TouchPanel.GetState();
                #region processing
                //                 while (TouchPanel.IsGestureAvailable)
                //                 { 
                //                     Deployment.Current.Dispatcher.BeginInvoke(() => getCurent()); 
                // 
                //                     GestureSample gs = TouchPanel.ReadGesture();
                //                     //TouchPanel.GetState().
                //                     switch (gs.GestureType)
                //                     {
                //                         case GestureType.VerticalDrag:
                //                             verticalDragStarted = true;
                //                             
                //                             Debug.WriteLine("   +vertical drag catched\n");
                // 
                //                             break;
                // 
                //                         case GestureType.Flick:
                //                             Debug.WriteLine("   +flick catched\n");
                //                             var da = Math.Abs(gs.Delta.X) + Math.Abs(gs.Delta.Y);
                //                             var dn = Math.Abs(gs.Delta2.X) + Math.Abs(gs.Delta2.Y);
                // 
                //                             if (da > 0 && dn > 0 )
                //                             {
                //                                 Debug.WriteLine("   +flick2x catched\n");
                //                             }
                // 
                //                             break;
                // 
                //                         case GestureType.Tap:
                //                             Debug.WriteLine("   +tap cathced\n");
                //                                                         
                //                             break;
                // 
                //                         case GestureType.PinchComplete:
                //                             Debug.WriteLine("   +pinch catched\n");
                //                             
                //                             break;
                // 
                //                         case GestureType.HorizontalDrag:
                //                             horizontalDragStarted = true;
                //                             Debug.WriteLine("   +horizontal drag catched\n");
                // 
                //                             break;
                // 
                //                         case GestureType.Hold:
                //                             Debug.WriteLine("   +hold catched\n");
                // 
                //                             break;
                // 
                //                         case GestureType.DragComplete:
                //                             if(horizontalDragStarted)
                //                             {
                //                                 Debug.WriteLine("<- horiz. ended\n");
                //                             }
                //                             if(verticalDragStarted)
                //                             {
                //                                 Debug.WriteLine("<- vert. ended\n");
                //                             }
                //                             horizontalDragStarted = false;
                //                             verticalDragStarted = false;
                //                             Debug.WriteLine("   +hold catched\n");
                // 
                //                             break;
                //                     }
                //                 } of while
                #endregion
            }
        }// end of update loop


        internal static byte[] getSessionStartDate()
        {
            return BitConverter.GetBytes(mSessionStartTime);
        }

        internal static byte[] getSessionEndDate()
        {
            var date = DateTime.Now;

            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            double sec = Math.Floor(diff.TotalSeconds);

            return BitConverter.GetBytes(sec);
        }

        internal static byte[] getUDID()
        {
            return mIDGen.UDID;
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

        private static byte[] getBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string getString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
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
                RegionInfo rf = new RegionInfo(cult.TwoLetterISOLanguageName);

                string lt3 = Converter.convertToThreeLetterCode( rf.TwoLetterISORegionName );

                return new byte[3] { (byte)lt3[0], (byte)lt3[1], (byte)lt3[2] };
            }
        }
    }
}
