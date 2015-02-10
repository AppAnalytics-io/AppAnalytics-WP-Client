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



namespace TouchLib
{
    public static class Detector
    {
        //private section /////////////////////////////////////////////////
        private static bool mKeepWorking = true;

        private static UUID.UDIDGen mIDGen = UUID.UDIDGen.Instance;

        private static Thread mWorker = null;
        private static Sender mSender = new Sender();

        public static string getResolutionX()
        {
            return "";
        }
        public static string getResolutionY()
        {
            return "";
        }
        public static byte ApiVersion = 1;

        //public section //////////////////////////////////////////////////
        static public String getSessionID()
        {
            return mIDGen.UUIDV4;
        }

        static public String getCurent()
        { 
            var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;

            var uri = currentPage.NavigationService.CurrentSource;

            return uri.ToString();
        }

        static public void init()
        {
            if (null == mWorker)
            {
                TouchPanel.EnabledGestures = GestureType.VerticalDrag | GestureType.HorizontalDrag | GestureType.Flick
                    | GestureType.Pinch | GestureType.Hold | GestureType.Tap | GestureType.DoubleTap;

                mWorker = new Thread(updateLoop);
                mWorker.IsBackground = true;
                mWorker.Start();
            }
        }

        static public void terminate()
        {
            mKeepWorking = false;
        }

        static private void updateLoop()
        {
            bool horizontalDragStarted = false;
            bool verticalDragStarted = false;

            while (mKeepWorking)
            {
                var gestres = TouchPanel.GetState();
                while (TouchPanel.IsGestureAvailable)
                {
                    GestureSample gs = TouchPanel.ReadGesture();
                    //TouchPanel.GetState().
                    switch (gs.GestureType)
                    {
                        case GestureType.VerticalDrag:
                            verticalDragStarted = true;
                            Debug.WriteLine("   +vertical drag catched\n");
                            break;

                        case GestureType.Flick:
                            Debug.WriteLine("   +flick catched\n");
                            break;

                        case GestureType.Tap:
                            Debug.WriteLine("   +tap cathced\n");
                            
                            break;

                        case GestureType.PinchComplete:
                            Debug.WriteLine("   +pinch catched\n");
                            
                            break;

                        case GestureType.HorizontalDrag:
                            horizontalDragStarted = true;
                            Debug.WriteLine("   +horizontal drag catched\n");

                            break;

                        case GestureType.Hold:
                            Debug.WriteLine("   +hold catched\n");

                            break;

                        case GestureType.DragComplete:
                            if(horizontalDragStarted)
                            {
                                Debug.WriteLine("<- horiz. ended\n");
                            }
                            if(verticalDragStarted)
                            {
                                Debug.WriteLine("<- vert. ended\n");
                            }
                            horizontalDragStarted = false;
                            verticalDragStarted = false;
                            Debug.WriteLine("   +hold catched\n");

                            break;
                    }
                }
            } //end of while
   
        }// end of update loop


        internal static ulong getSessionStartDate()
        {
            throw new NotImplementedException();
        }

        public static bool getSessionEndDate()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public static bool getUDID()
        {
            throw new Exception("The method or operation is not implemented.");
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

        public static string AppVersion 
        {
            get 
            {
                byte[] bts = new byte[16];
                string[] v4 = getAppVersion().Split('.');

                if (v4.Length != 4) 
                {
                    return "00000000" + "00000000"; //16*0
                }

                int i = 0;
                foreach (var v in v4)
                {
                    //bts[i] = (byte) v[0];
                    var parsed = BitConverter.GetBytes(UInt32.Parse(v));
                    System.Buffer.BlockCopy( parsed, 0, bts, i*4, parsed.Length);
                    ++i;
                }

                return getString(bts);
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

        public static string OSVersion 
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                var vs = Environment.OSVersion.Version;

                sb.Append( getString(toBytes(vs.Major)) )
                  .Append( getString(toBytes(vs.Minor)) )
                  .Append( getString(toBytes(vs.Build)) )
                  .Append( getString(toBytes(vs.Revision)) );

                if (sb.Length != 16)
                {
                    return "00000000" + "00000000"; //16*0
                }

                return sb.ToString();  
            }
        }

        public static string SystemLocale 
        {
            get 
            {
                CultureInfo cult = Thread.CurrentThread.CurrentCulture;
                RegionInfo rf = new RegionInfo(cult.TwoLetterISOLanguageName);

                return  rf.TwoLetterISORegionName + " " ;
            }
        }
    }
}
