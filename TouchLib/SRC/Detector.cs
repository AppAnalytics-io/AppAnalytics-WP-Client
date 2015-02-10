using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Input.Touch;
using System.Diagnostics;



namespace TouchLib
{
    public static class Detector
    {
        //private section /////////////////////////////////////////////////
        private static bool mKeepWorking = true;

        private static UUID.UDIDGen mIDGen =UUID.UDIDGen.Instance;

        private static Thread mWorker = null;
        private static Sender mSender = new Sender();

        //public section //////////////////////////////////////////////////
        static public String getSessionID()
        {
            return mIDGen.UUIDV4;
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

        // private func
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
        
    }
}
