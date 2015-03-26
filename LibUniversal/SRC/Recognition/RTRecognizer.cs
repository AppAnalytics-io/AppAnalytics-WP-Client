using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AppAnalytics.ShakeGestures;
using Windows.ApplicationModel.Core;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using TouchPointCollection = System.Collections.ObjectModel.Collection<AppAnalytics.TouchPoint>;

namespace AppAnalytics
{
    internal class RTRecognizer
    {
        TouchPointCollection    mTPC = new TouchPointCollection();

        uint                    mLastFrame = 0; //id of last frame 
        Dictionary<uint, Dictionary<uint, TouchPoint>> mTouches = new Dictionary<uint, Dictionary<uint, TouchPoint>>();

        /* NOTE
         * I'm not using only GestureRecognizer for couple of reasons.
         * First of all this class has very limited amount of supported gestures
         * and its work is not clean.
         * Second reason is that it can't track pointers that already
         * being tracked ->
         * https://social.msdn.microsoft.com/Forums/windowsapps/en-US/ceb4a3dc-05f4-4361-bbf7-039310a5d0d3/failed-to-start-tracking-the-pointer-because-it-is-already-being-tracked?forum=winappswithcsharp
         * it being used for basic manipulations
         * also it can't recognize 2-3-4 fingers taps. did it by my own.
         */
        private GestureRecognizer mRecognizer = new GestureRecognizer();

        private readonly object _lockObj = new object();
        private Stopwatch mStopWatch = new Stopwatch();

        private const bool kUseRecognizer = true;
        private const float kOneGestureTimeTolerance = 0.08f;

        const float kTimeToUpdateFingersCount = 50; //ms
        bool        mIsIteriaSubmited = false;
        int         mStepsWaiting = 0;
        const int   kStepsBeforeStart = 4; // temporary solution. may be changed any time soon.
                                          // added 'cause of specific manipulationUpdated firing mechanism
        const float kSwipeIteriaThreshold = 1.0f;

        private int mFingers = 0;
        public int Fingers
        {
            get
            {
                lock (_lockObj) return mFingers;
            }
            set
            {
                lock (_lockObj) mFingers = value;
            }
        }
        private int mPrevFingers = 0;
        public int PrevFingers
        {
            get
            {
                lock (_lockObj) return mPrevFingers;
            }
            set
            {
                lock (_lockObj) mPrevFingers = value;
            }
        }

        private static RTRecognizer mInstance = null;
        private double mStartX;
        private double mStartY;
        protected RTRecognizer()
        {   }

        public static RTRecognizer Instance
        {
            get { return mInstance ?? (mInstance = new RTRecognizer()); }
        }

        void instanceShakeGesture(object sender, ShakeGestureEventArgs e)
        {
            //Debug.WriteLine("shaking");
            if (e.ShakeType == ShakeType.X)
            {
                //to-do -> set shake orientation
            }
            else if (e.ShakeType == ShakeType.Y)
            {
                //to-do -> set shake orientation
            }
            else
            {
                //to-do -> set shake orientation
            }
            GestureProcessor.createGesture(GestureID.Shake);
        }

        public void init()
        {
            var frame = Window.Current.Content as Frame;
            CoreWindow window = CoreApplication.MainView.CoreWindow;

            mRecognizer.GestureSettings = GestureSettings.Tap | GestureSettings.Hold
                | GestureSettings.ManipulationMultipleFingerPanning
                | GestureSettings.ManipulationScale
                | GestureSettings.ManipulationTranslateX
                | GestureSettings.ManipulationTranslateY
                | GestureSettings.Drag
                | GestureSettings.ManipulationRotate
 //               | GestureSettings.ManipulationRotateInertia
                | GestureSettings.ManipulationTranslateInertia;

            /*
             * NOTE : Build-in accelerator event Shaken will NEVER be fired on WP platform.
             * I'm using custom code fro this. You can find proof on msdn (class Accelerometer, event Shaken)
             * https://msdn.microsoft.com/en-us/library/windows/apps/windows.devices.sensors.accelerometer.shaken
             */

            ShakeGesturesHelper.Instance.ShakeGesture +=
                new EventHandler<ShakeGestureEventArgs>(instanceShakeGesture);
            ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 4;
            ShakeGesturesHelper.Instance.Active = true;

            mRecognizer.Dragging += dragging;
            mRecognizer.Tapped += tap;
            mRecognizer.Holding += hold;
            mRecognizer.CrossSliding += crossSliding;
            mRecognizer.ManipulationStarted += this.manipulationStarted;
            mRecognizer.ManipulationUpdated += this.manipulationUpdated;
            mRecognizer.ManipulationCompleted += this.manipulationCompleted;
            mRecognizer.ManipulationInertiaStarting += this.manipulationInertiaStarted;
            //mRecognizer.

            window.IsInputEnabled = true;

            var view = CoreApplication.MainView;
            bool isMain = view.IsMain;
//             if (!kUseRecognizer) // still waiting for MSDN staff to fix strange CoreWindow behavior.
//             {
//                 window.PointerPressed += _pointerPressed;
//                 window.PointerReleased += _pointerReleased;
//                 window.PointerMoved += _pointerMoved;
//                 window.PointerExited += _pointerReleased;
//                 window.PointerEntered += _pointerPressed;
//                 window.PointerCaptureLost += _pointerReleased;
//             }
//             else
            {
                frame.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(onPPressed), true);
                frame.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(onPMoved), true);
                frame.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(onPUP), true);
                frame.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(onPUP), true);
                frame.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(onPUP), true);
                frame.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(onPUP), true);
            }
        }

        private void onPPressed(object sender, PointerRoutedEventArgs e)
        {
            var ps = e.GetIntermediatePoints(null);
            var source = e.OriginalSource;
            if (ps != null && ps.Count > 0)
            {
                if (source as Control != null)
                {
                    GestureProcessor.ElementURI = (source as Control).Name;
                }
                else if (source as FrameworkElement != null)
                {
                    GestureProcessor.ElementURI = (source as FrameworkElement).Name;
                }

                lock (_lockObj)
                {
                    mStartX = ps[0].Position.X;
                    mStartY = ps[0].Position.Y;
                }

                try
                {
                    this._pointerPressed(ps[0]);
                    mRecognizer.ProcessDownEvent(ps[0]);
                }
                catch (Exception) { }
            }
        }

        private void onPMoved(object sender, PointerRoutedEventArgs e)
        {
            var ps = e.GetIntermediatePoints(null);
            if (ps != null && ps.Count > 0)
            {
                try
                {
                    this._pointerMoved(ps[0]);
                    mRecognizer.ProcessMoveEvents(ps);
                }
                catch (Exception) { }
            }
        }

        private void onPUP(object sender, PointerRoutedEventArgs e)
        {
            var ps = e.GetIntermediatePoints(null);
            if (ps != null && ps.Count > 0)
            {
                this._pointerReleased(ps[0]);
                try
                {
                    mRecognizer.ProcessUpEvent(ps[0]);
                    mRecognizer.CompleteGesture();
                }
                catch (Exception) { }
            }
            if (ps != null && ps.Count == 1)
            {
                mStopWatch.Stop();
                mStopWatch.Reset();
            }
        }

        void dragging(GestureRecognizer sender, DraggingEventArgs args)
        {
        }

        void tap(GestureRecognizer sender, TappedEventArgs args)
        {
        }

        void hold(GestureRecognizer sender, HoldingEventArgs args)
        {
        }

        void crossSliding(GestureRecognizer sender, CrossSlidingEventArgs args)
        {
        }

        void manipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
            mStepsWaiting = 0;
        }

        void manipulationInertiaStarted(GestureRecognizer sender, ManipulationInertiaStartingEventArgs args)
        {
            var x = args.Velocities.Linear.X;
            var y = args.Velocities.Linear.Y;
            float len = (new Vector2((float)x, (float)y)).Length();

            //Debug.WriteLine("inertia started -> " + args.Velocities.Linear);
            if (Fingers == 0 && kSwipeIteriaThreshold < len && GestureProcessor.isMovingState())
            {
                GestureProcessor.createSwipeFromState(PrevFingers, true);
                mIsIteriaSubmited = true;
            }
        }

        void manipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            if (kStepsBeforeStart > mStepsWaiting && !(Fingers == 2 && args.Delta.Expansion != 0))
            {
                mStepsWaiting ++;
                return;
            }
            else if (Fingers == 2 && args.Delta.Expansion != 0)
            {
                mStepsWaiting = kStepsBeforeStart;
            }

            if (mStopWatch.ElapsedMilliseconds > kTimeToUpdateFingersCount)
            {
                PrevFingers = Fingers;
                mStopWatch.Restart();
            }
            lock (_lockObj)
            {
                if (mTouches.ContainsKey(mLastFrame))
                {
                    Fingers = mTouches[mLastFrame].Count;
                }
            }

           GestureProcessor.updateState(args.Delta, args.Cumulative);
        }

        void manipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            if (mIsIteriaSubmited)
            {
                mIsIteriaSubmited = false;
                return;
            }
            var x = args.Velocities.Linear.X;
            var y = args.Velocities.Linear.Y;
            float len = (new Vector2((float)x, (float)y)).Length();

            if ( (kSwipeIteriaThreshold < (len*0.7)) && GestureProcessor.isMovingState())
            {
                GestureProcessor.createSwipeFromState(PrevFingers, true);
            }
            else
            {
                GestureProcessor.createGestureFromState(PrevFingers, true, args.Cumulative);
            }
        }

        void pushIntoCollection(TouchPoint tp, uint pointerID, uint frameID)
        {
            lock (_lockObj)
            {
                if (mTouches.ContainsKey(frameID) && mTouches[frameID].ContainsKey(pointerID))
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

        void changeFrameInfo(TouchPoint tp, uint pointerID, uint frameID)
        {
            var flag = (mTouches.ContainsKey(mLastFrame) && mTouches[mLastFrame].Count == 1 &&  tp.Action == TouchAction.Up);
            if (flag)
            {
                pushIntoCollection(tp, pointerID, mLastFrame);
            }

            if ( (mLastFrame != frameID && mTouches.ContainsKey(mLastFrame)))
            {
                ////Debug.WriteLine("[frame] : "+ frameID);
                foreach ( var it in mTouches[mLastFrame])
                {
                    mTPC.Add(it.Value);
                }

                int counter = 0;
                foreach (var it in mTPC)
                {
                    if (it.Action != TouchAction.Up) counter++;
                }

                Fingers = counter <= 4 ? counter : 4;

                FrameProcessor.Instance.manipulationFrame(mTPC);
                GestureProcessor.updateState(new ManipulationDelta(), new ManipulationDelta(), true);

                mTPC.Clear();
                lock (_lockObj)
                {
                    mTouches.Remove(mLastFrame);
                }
                mLastFrame = frameID;

                if (flag) return;
            }
            else { mLastFrame = frameID; }

            if (!flag)
            {
                pushIntoCollection(tp, pointerID, frameID);
            }
        }

        private void _pointerPressed(PointerPoint point)
        {
            if (mPointerStatus.ContainsKey(point.PointerId))
            {
                mPointerStatus[point.PointerId] = true;
            }
            else
            {
                mPointerStatus.Add(point.PointerId, true);
            }

            var CurrentPoint = point;
            if (mStopWatch.IsRunning == false)
            {
                mStopWatch.Start();
            }

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Down;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerMoved(PointerPoint point)
        {
            var CurrentPoint = point;

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Move;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        Dictionary<uint, bool> mPointerStatus = new Dictionary<uint, bool>();

        private void _pointerReleased(PointerPoint point)
        {
            if (mPointerStatus.ContainsKey(point.PointerId) && true == mPointerStatus[point.PointerId])
            {
                var CurrentPoint = point;
                TouchPoint tp = new TouchPoint();
                tp.Action = TouchAction.Up;
                tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

                mPointerStatus[point.PointerId] = false;

                changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
            }
            else if (mPointerStatus.ContainsKey(point.PointerId))
            {
                mPointerStatus.Remove(point.PointerId);
            }
        }

        // app-level handlers - working with raw data
        #region unused
        private void _pointerPressed(object sender, PointerEventArgs e)
        {
            var CurrentPoint = e.CurrentPoint;

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Down;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerMoved(object sender, PointerEventArgs e)
        { 
            Debug.WriteLine("<<Pointer moved>>"); 
            var CurrentPoint = e.CurrentPoint;

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Move;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }

        private void _pointerReleased(object sender, PointerEventArgs e)
        {
            var CurrentPoint = e.CurrentPoint;

            TouchPoint tp = new TouchPoint();
            tp.Action = TouchAction.Up;
            tp.X = (float)CurrentPoint.Position.X; tp.Y = (float)CurrentPoint.Position.Y;

            changeFrameInfo(tp, CurrentPoint.PointerId, CurrentPoint.FrameId);
        }
        #endregion

        public double StartY
        {
            get { lock (this._lockObj) { return mStartX; } }
        }
        public double StartX
        {
            get { lock (this._lockObj) { return mStartY; } }
        }
    }
}
