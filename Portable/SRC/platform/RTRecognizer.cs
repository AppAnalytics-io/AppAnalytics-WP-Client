using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AppAnalitics;

using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using TouchPointCollection = System.Collections.ObjectModel.Collection<AppAnalitics.TouchPoint>;

namespace AppAnalytics 
{
    internal class RTRecognizer
    {
 //       private GestureRecognizer mRecognizer = null;
        private readonly object _lockObj = new object();

        private static RTRecognizer mInstance = null;
        protected RTRecognizer()
        {   }

        public static RTRecognizer Instance
        {
            get { return mInstance ?? (mInstance = new RTRecognizer()); }
        }

        private void testing (CoreWindow sender, TouchHitTestingEventArgs e)
        {
            Debug.WriteLine(e.Point.ToString());
        }
        public void init()
        {
            var currentPage = Window.Current.Content as Frame;
            CoreWindow window = CoreApplication.MainView.CoreWindow;
            //window.TouchHitTesting += testing;
            
            window.IsInputEnabled = true;
            //window.
//             if (useRouted)
//             {
//                 currentPage.PointerPressed += _pointerPressedRouted;
//                 currentPage.PointerReleased += _pointerReleasedRouted;
//                 //currentPage.PointerMoved += _pointerMovedRouted;
//                 currentPage.PointerExited += _pointerExitedRouted;
//                 currentPage.PointerEntered += _pointerPressedRouted;
// 
//                 //currentPage.ManipulationStarted += manipulationStarted;
//                 //ManipulationStartedEventHandler p = new ManipulationStartedEventHandler(  //(manipulationStarted);
//                 currentPage.PointerCanceled += _pointerExitedRouted;
//                 currentPage.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(_pointerMovedRouted), true);
//             } 
            window.PointerPressed += _pointerPressed;
            window.PointerReleased += _pointerReleased;
            window.PointerMoved += _pointerMoved;
            window.PointerExited += _pointerExited;
            window.PointerEntered += _pointerPressed;
            window.PointerCaptureLost += _pointerLost;  
        }

        TouchPointCollection mTPC = new TouchPointCollection();
         
        uint mLastFrame = 0;

        Dictionary<uint, Dictionary<uint, TouchPoint>> mTouches = new Dictionary<uint, Dictionary<uint, TouchPoint>>();
         

        void changeFrameInfo(TouchPoint tp, uint pointerID, uint frameID)
        {
            if (mLastFrame != frameID && mTouches.ContainsKey(mLastFrame))
            {
                Debug.WriteLine("[frame] : "+ frameID);
                foreach ( var it in mTouches[mLastFrame])
                {
                    mTPC.Add(it.Value);
                }
                Recognizer.Instance.manipulationFrame(mTPC);

                mTPC.Clear();
                mTouches.Remove(mLastFrame);
                mLastFrame = frameID;
            }
            else { mLastFrame = frameID; }

            lock (_lockObj)
            {
                if (mTouches.ContainsKey(frameID) && mTouches[frameID].ContainsKey(pointerID) )
                {
                    mTouches[frameID][pointerID] = tp;
                }
                else if (mTouches.ContainsKey(frameID))
                {
                    mTouches[frameID].Add(pointerID, tp);
                }
                else
                {
                    mTouches.Add(frameID, new Dictionary<uint, TouchPoint>());
                    mTouches[frameID].Add(pointerID, tp);
                }
            }
        } 

        private void _pointerPressed(object sender, PointerEventArgs e)
        { 
            Debug.WriteLine("pressed");
            var CurrentPoint = e.CurrentPoint; 

            TouchPoint tp = new TouchPoint(); 
            tp.Action = TouchAction.Down;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;
            
            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerMoved(object sender, PointerEventArgs e)
        {
            var CurrentPoint = e.CurrentPoint;
            Debug.WriteLine(".");
            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Move;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;
            
            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerReleased(object sender, PointerEventArgs e)
        {
            var CurrentPoint = e.CurrentPoint;
            Debug.WriteLine("released");

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Up;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;
            
            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerExited(object sender, PointerEventArgs e)
        { 
            var CurrentPoint = e.CurrentPoint;
            Debug.WriteLine("exited");

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Up;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;
            
            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerLost(object sender, PointerEventArgs e)
        {
            var CurrentPoint = e.CurrentPoint;
            Debug.WriteLine("capture lost");
        }
    }
}
