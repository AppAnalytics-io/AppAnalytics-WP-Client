using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        { }

        public static RTRecognizer Instance
        {
            get { return mInstance ?? (mInstance = new RTRecognizer()); }
        }
        CoreWindow __window = null;
        public void init()
        {
            var currentPage = Window.Current.Content as Frame;

            var p = currentPage.BaseUri;
            var ct = currentPage.Content;
            //Window w; w.CoreWindow.
//             mRecognizer = new GestureRecognizer()
//             {
//                 GestureSettings =   GestureSettings.Tap  | GestureSettings.RightTap | GestureSettings.DoubleTap     /* | GestureSettings.ManipulationTranslateY |
//                                     GestureSettings.Hold      | GestureSettings.ManipulationTranslateX |
//                                     GestureSettings.Drag      | GestureSettings.ManipulationTranslateInertia |
//                                     GestureSettings.DoubleTap | GestureSettings.ManipulationRotate|
//                                     GestureSettings.CrossSlide| GestureSettings.ManipulationScale  */
//             };

            CoreWindow window = CoreApplication.MainView.CoreWindow;
            __window = window;
            const bool useRouted = true;
            window.InputEnabled += (CoreWindow sender, InputEnabledEventArgs e) =>
                {
                    int g = 20;
                };




            window.IsInputEnabled = true;
            //window.
            if (useRouted)
            {
                currentPage.PointerPressed += _pointerPressedRouted;
                currentPage.PointerReleased += _pointerReleasedRouted;
                //currentPage.PointerMoved += _pointerMovedRouted;
                currentPage.PointerExited += _pointerExitedRouted;
                currentPage.PointerEntered += _pointerPressedRouted;

                //currentPage.ManipulationStarted += manipulationStarted;
                //ManipulationStartedEventHandler p = new ManipulationStartedEventHandler(  //(manipulationStarted);
                currentPage.PointerCanceled += _pointerExitedRouted;
                currentPage.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(_pointerMovedRouted), true);
            }
            else
            {
                //var t = window.ge;
                //window.SetPointerCapture();
                window.PointerPressed += _pointerPressed;
                window.PointerReleased += _pointerReleased;
                window.PointerMoved += _pointerMoved;
                window.PointerExited += _pointerExited;
                window.PointerEntered += _pointerPressed;

                 
                window.PointerCaptureLost += _pointerExited;
                //window.PointerPressed
            } 
            
        }

//         void manipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
//         {
//             Debug.WriteLine("---");
//         }

        TouchPointCollection mTPC = new TouchPointCollection();

        //bool mFrameChanged = false;
        uint mLastFrame = 0;

        Dictionary<uint, Dictionary<uint, TouchPoint>> mTouches = new Dictionary<uint, Dictionary<uint, TouchPoint>>();

//         private void updateFrame()
//         {
//             lock (_lockObj)
//             {
//                 mTPC.Clear();
//             }
//         } 

        void changeFrameInfo(TouchPoint tp, uint pointerID, uint frameID)
        {
            if (__window != CoreApplication.MainView.CoreWindow)
            {
                Debug.Assert(false, "holy shit");
            }
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

        private void _pointerPressedRouted(object sender, PointerRoutedEventArgs e)
        {  
            var CurrentPoint = e.GetCurrentPoint(null);

            Debug.WriteLine("Pressed");
            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Down;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerMovedRouted(object sender, PointerRoutedEventArgs e)
        {
            var CurrentPoint = e.GetCurrentPoint(null);
            Debug.WriteLine("<>");
            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Move;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerReleasedRouted(object sender, PointerRoutedEventArgs e)
        {
            var CurrentPoint = e.GetCurrentPoint(null);

            Debug.WriteLine("RELEASED");
            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Up;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerExitedRouted(object sender, PointerRoutedEventArgs e)
        {
            var CurrentPoint = e.GetCurrentPoint(null);

            Debug.WriteLine("EXITED");
            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Up;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        //---------------------------------------/
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
//         void grPointerReleased(object sender, PointerRoutedEventArgs e)
//         {
//         }
// 
//         void grPointerMoved(object sender, PointerRoutedEventArgs e)
//         {
//             mRecognizer.ProcessMoveEvents(e.GetIntermediatePoints(null));
//             e.Handled = true;
//         }
// 
//         void grPointerPressed(object sender, PointerRoutedEventArgs e)
//         {
//             var ps = e.GetIntermediatePoints(null);
//             if (ps != null && ps.Count > 0)
//             {
//                 mRecognizer.ProcessDownEvent(ps[0]);
//                 e.Handled = true;
//             }
        //         }

        //         private void onRotate(GestureRecognizer gr, InertiaRotationBehavior e)
        //         {
        // 
        //         }

        //         private void onHolding(GestureRecognizer gr, HoldingEventArgs e)
        //         {
        //             Debug.WriteLine(" holding ");
        //         }
        // 
        //         private void onCrossSliding(GestureRecognizer gr, CrossSlidingEventArgs e)
        //         {
        //             Debug.WriteLine(" swipe ");
        //         }
        // 
        //         private void onTapping(GestureRecognizer gr, TappedEventArgs e)
        //         {
        //             Debug.WriteLine(" tap ");
        //         }

    }
}
