using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AppAnalytics
{
    internal static class GestureProcessor
    {
        public enum GState
        {
            MovingUp = 0,
            MovingDown,
            MovingLeft,
            MovingRight,

            Enlarge,
            Shrink,

            RotateC,
            RotateAC,

            Hold,

            FingersUp,
            FingerDown,

            None
        }
        const double kTimeForHold = 0.48f;

        const double kHoldThreshold = 0.42;
        const double kSwipeThreshold = 1.7;

        static Stopwatch mTimeMark = new Stopwatch();

        static double HoldThreshold
        {
            get
            {
               // var x = new Vector2((float)Detector.getResolutionXDouble(), (float)Detector.getResolutionYDouble());
                return /*x.Length() **/ kHoldThreshold;
            }
        }
        static double SwipeThreshold
        {
            get
            {
               // var x = new Vector2((float)Detector.getResolutionXDouble(), (float)Detector.getResolutionYDouble());
                return /*x.Length() **/ kSwipeThreshold;
            }
        }

        private static readonly object _lockObject = new object();

        static string mBufferPageUri = "none";
        static string mBufferElementUri = "none";
        public static string ElementURI
        {
            set
            {
                lock (_lockObject)
                {
                    if (value != "")
                        mBufferElementUri = value; 
                    else
                        mBufferElementUri = "none"; 
                }
            }
            get
            {
                lock (_lockObject) { return mBufferElementUri; }
            }
        }
         
        static GState mState = GState.None;
        static public GState State
        {
            get { return mState; }
        }

        static public  bool isMovingState( )
        {
            return (mState == GState.MovingUp) || (mState == GState.MovingDown)
                || (mState == GState.MovingLeft) || (mState == GState.MovingRight);
        }

        static public bool isRotateState( )
        {
            return (mState == GState.RotateC) || (mState == GState.RotateAC);
        }

        static public bool isZoomState( )
        {
            return (mState == GState.Shrink) || (mState == GState.Enlarge);
        }

        static int mPrevFingersCnt = 0;
        static public void updateState(float aScale, float aRotation, Point aDelta, float aRelativeScale, bool checkHoldOnly = false)
        {
            bool flag = !isMovingState() && !isRotateState() && !isZoomState();
            if ((checkHoldOnly && flag) && mTimeMark.IsRunning && mTimeMark.ElapsedMilliseconds > kTimeForHold * 1000)
            {
                mTimeMark.Reset();
                mState = GState.Hold;

                if (RTRecognizer.Instance.Fingers > 0)
                {
                    createHoldGesture(RTRecognizer.Instance.Fingers);
                    Debug.WriteLine("[hold] " + RTRecognizer.Instance.Fingers);
                }
            }
            else if (!flag) mTimeMark.Reset();

            if (GState.None == mState )
            {
                if (mPrevFingersCnt != RTRecognizer.Instance.Fingers)
                {
                    if (mTimeMark.IsRunning == false)
                    {
                        mTimeMark.Restart(); 
                    }
                }
                else if (!checkHoldOnly)
                {
                    selectState(aScale, aRotation, aDelta, aRelativeScale);
                }
            }
            else if (GState.Hold == mState)
            {
                if (mPrevFingersCnt != RTRecognizer.Instance.Fingers)
                {
                    if (mTimeMark.IsRunning == false)
                    {
                        mTimeMark.Start();
                        return;
                    }
                    else
                    {
                        if (mTimeMark.ElapsedMilliseconds > kTimeForHold * 1000)
                        {
                            createHoldGesture(RTRecognizer.Instance.Fingers);
                            Debug.WriteLine("[-hold->] " + RTRecognizer.Instance.Fingers);
                            mTimeMark.Stop();
                            mTimeMark.Reset();
                        }
                    }
                }
                else
                {
                    selectState(aScale, aRotation, aDelta, aRelativeScale);
                }
            }
            else if (isZoomState())
            {
                if (aScale < 0 && mState != GState.Shrink)
                {
                    Debug.WriteLine("[Enlarge > shrink]");
                    // todo - convert
                    createGesture(GestureID.ZoomWith2Finger, BitConverter.GetBytes(aRelativeScale));
                    mState = GState.Shrink;
                    // push event
                }
                else if (aScale > 0 && mState != GState.Enlarge)
                {
                    Debug.WriteLine("[Shrink > enlarge]");
                    createGesture(GestureID.PinchWith2Finger, BitConverter.GetBytes(aRelativeScale));
                    mState = GState.Enlarge;
                }
            }
            else if (isRotateState())
            {
                // reserved. pass
            }
            else if (isMovingState())
            {
                // reserved. pass
            }
            mPrevFingersCnt = RTRecognizer.Instance.Fingers;
        }

        static public void createGestureFromState(int aFingers, bool aReset, ManipulationDelta aDelta)
        { 
            if (isZoomState())
            {
                Debug.WriteLine("[zoom]");
                if (GState.Enlarge == mState)
                {
                    createGesture(GestureID.ZoomWith2Finger, BitConverter.GetBytes( aDelta.Scale) );
                }
                else
                {
                    createGesture(GestureID.PinchWith2Finger, BitConverter.GetBytes(aDelta.Scale));
                }
            }
            else if (isRotateState())
            {
                Debug.WriteLine("[rotate]");
                createGesture(GestureID.RotateWith2Finger, BitConverter.GetBytes(aDelta.Rotation));
            }
            else if (isMovingState())
            {
                Debug.WriteLine("[flick]" + aFingers);
                createFlickGesture(aFingers, mState);
            }

            if (aReset)
            {
                mState = GState.None;
            }
        }

        static public void createSwipeFromState(int aFingers, bool aReset = true)
        {
            if (isMovingState())
            {
                Debug.WriteLine("[[swipe]]" + aFingers);
                createSwipeGesture(aFingers, mState);
            }

            if (aReset)
            {
                mState = GState.None;
            }
        }

        const float    kZoomMetricCf   = 1.5f;
        const UInt16    kRotateMetricCf = 8;
        const float     kMovingMetricCf = 1;

        static public void selectState(float aScale, float aRotation, Point aDelta, float aRelative)
        {
            var dbg = RTRecognizer.Instance.Fingers;

            var moveMetric      = moveNormalized(aDelta);
            var zoomMetric      = checkForZoom(aScale);
            var rotationMetric  = checkForRotation(aRotation);

            var tmp = Math.Max(moveMetric, zoomMetric);
            if (HoldThreshold > Math.Max(tmp, rotationMetric))
            {
                if (mTimeMark.IsRunning == false && mState != GState.Hold)
                {
                    mTimeMark.Start();
                    return;
                }
                else return;
            } 

            if ((zoomMetric > rotationMetric) && (zoomMetric > moveMetric))
            {
                if (mTimeMark.IsRunning == true)
                {
                    mTimeMark.Reset();
                }
                Debug.WriteLine(".zoom");
                if (aScale < 0)
                {
                    mState = GState.Shrink;
                }
                else
                {
                    mState = GState.Enlarge;
                }
            }
            else if ((rotationMetric > zoomMetric) && (rotationMetric > moveMetric))
            {
                if (mTimeMark.IsRunning == true)
                {
                    mTimeMark.Reset();
                }
                Debug.WriteLine(".rotation");
                if (aRotation > 0)
                {
                    mState = GState.RotateC;
                }
                else
                {
                    mState = GState.RotateAC;
                }
            }
            else
            {
                if (mTimeMark.IsRunning == true)
                {
                    mTimeMark.Reset();
                }
                Debug.WriteLine(".movement");

                mState = stateByDirection(aDelta);
            }
        }

        static GState stateByDirection(Point vec)
        {
            if ( Math.Abs(vec.X) > Math.Abs(vec.Y) )
            {
                if (vec.X < 0)
                {
                    return GState.MovingLeft;
                }
                else
                {
                    return GState.MovingRight;
                }
            }
            else
            {
                if (vec.Y < 0)
                {
                    return GState.MovingUp;
                }
                else
                {
                    return GState.MovingDown;
                }
            }
        }

        static double moveNormalized(Point aDelta)
        {
            //var rv = new Vector2((float)Detector.getResolutionXDouble(), (float)Detector.getResolutionYDouble());
            float len = (new Vector2((float)aDelta.X, (float)aDelta.Y)).Length();
            //len = len / rv.Length();

            return kMovingMetricCf * len;// 100 -> percent
        }

        static double checkForZoom(double distDif)
        {
            if (RTRecognizer.Instance.Fingers < 2) return 0;
            double metric = distDif;
    
//             distDif = Math.Abs(distDif);
//             if (distDif > 0)
//             {
//                 var vec = new Vector2(  (float)Detector.getResolutionXDouble(), 
//                                         (float)Detector.getResolutionYDouble());
// 
//                 distDif = (distDif / vec.Length()) * 100;  // 100 -> percent
// 
//                 metric = kZoomMetricCf * distDif;
//             }

            return metric * kZoomMetricCf;
        }

        static double checkForRotation(double diff)
        {
            if (RTRecognizer.Instance.Fingers < 2) return 0;
            double metric = 0;
             
            //preAngle = nowAngle;
            metric = (diff / 360.0f) * 100 * kRotateMetricCf; // 100 -> percent
            

            return metric;
        }

        static public void createTapGesture(int aCountOfTaps, int aNumberOfFingers)
        {
            GestureID id = GestureID.SingleTapWith1Finger;

            if (1 == aNumberOfFingers)
            {
                if (1 == aCountOfTaps) id = GestureID.SingleTapWith1Finger;
                if (2 == aCountOfTaps) id = GestureID.DoubleTapWith1Finger;
                if (3 == aCountOfTaps) id = GestureID.TripleTapWith1Finger;
            }
            else if (2 == aNumberOfFingers)
            {
                if (1 == aCountOfTaps) id = GestureID.SingleTapWith2Finger;
                if (2 == aCountOfTaps) id = GestureID.DoubleTapWith2Finger;
                if (3 == aCountOfTaps) id = GestureID.TripleTapWith2Finger;
            }
            else if (3 == aNumberOfFingers)
            {
                if (1 == aCountOfTaps) id = GestureID.SingleTapWith3Finger;
                if (2 == aCountOfTaps) id = GestureID.DoubleTapWith3Finger;
                if (3 == aCountOfTaps) id = GestureID.TripleTapWith3Finger;
            }
            else if (4 == aNumberOfFingers)
            {
                if (1 == aCountOfTaps) id = GestureID.SingleTapWith4Finger;
                if (2 == aCountOfTaps) id = GestureID.DoubleTapWith4Finger;
                if (3 == aCountOfTaps) id = GestureID.TripleTapWith4Finger;
            }

            createGesture(id);
        }

        static public void createSwipeGesture(int aNumberOfFingers, GState aDir)
        {
            GestureID id = GestureID.SwipeDownWith1Finger;

            if (1 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.SwipeRightWith1Finger;
                if (GState.MovingLeft == aDir) id = GestureID.SwipeLeftWith1Finger;
                if (GState.MovingDown == aDir) id = GestureID.SwipeDownWith1Finger;
                if (GState.MovingUp == aDir) id = GestureID.SwipeUpWith1Finger;
            }
            else if (2 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.SwipeRightWith2Finger;
                if (GState.MovingLeft == aDir) id = GestureID.SwipeLeftWith2Finger;
                if (GState.MovingDown == aDir) id = GestureID.SwipeDownWith2Finger;
                if (GState.MovingUp == aDir) id = GestureID.SwipeUpWith2Finger;
            }
            else if (3 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.SwipeRightWith3Finger;
                if (GState.MovingLeft == aDir) id = GestureID.SwipeLeftWith3Finger;
                if (GState.MovingDown == aDir) id = GestureID.SwipeDownWith3Finger;
                if (GState.MovingUp == aDir) id = GestureID.SwipeUpWith3Finger;
            }
            else if (4 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.SwipeRightWith4Finger;
                if (GState.MovingLeft == aDir) id = GestureID.SwipeLeftWith4Finger;
                if (GState.MovingDown == aDir) id = GestureID.SwipeDownWith4Finger;
                if (GState.MovingUp == aDir) id = GestureID.SwipeUpWith4Finger;
            }

            createGesture(id);
        }

        static public async void createGesture(GestureID aID, byte[] param1 = null)
        { 
           await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal , () => getCurrentPage()); 

            Point p = new Point(RTRecognizer.Instance.StartX, RTRecognizer.Instance.StartY);

            GestureData gd = GestureData.create(aID, p, ElementURI, mBufferPageUri, param1);

            Detector.pushReport(gd);
        }

        static private void getCurrentPage()
        {
            try
            {
                var currentPage = (Window.Current.Content as Frame).Content as Page;
                //Frame.
                if (null == currentPage) return;
                var uri = currentPage.BaseUri.LocalPath;

                lock (_lockObject)
                {
                    mBufferPageUri = uri.ToString();
                } 
            }
            catch (Exception e)
            { /*Debug.WriteLine("curr.page (gp)" + e.ToString());*/ }
        }

        static private void createHoldGesture(int aNumberOfFingers)
        {
            GestureID id = GestureID.HoldWith1Finger;

            if (1 == aNumberOfFingers)
            {
                id = GestureID.HoldWith1Finger;
            }
            else if (2 == aNumberOfFingers)
            {
                id = GestureID.HoldWith2Finger;
            }
            else if (3 == aNumberOfFingers)
            {
                id = GestureID.HoldWith3Finger;
            }
            else if (4 == aNumberOfFingers)
            {
                id = GestureID.HoldWith4Finger;
            }

            createGesture(id);
        }

        static public void createFlickGesture(int aNumberOfFingers, GState aDir)
        {
            GestureID id = GestureID.SwipeDownWith1Finger;

            if (1 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.FlickRightWith1Finger;
                if (GState.MovingLeft == aDir) id = GestureID.FlickLeftWith1Finger;
                if (GState.MovingDown == aDir) id = GestureID.FlickDownWith1Finger;
                if (GState.MovingUp == aDir) id = GestureID.FlickUpWith1Finger;
            }
            else if (2 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.FlickRightWith2Finger;
                if (GState.MovingLeft == aDir) id = GestureID.FlickLeftWith2Finger;
                if (GState.MovingDown == aDir) id = GestureID.FlickDownWith2Finger;
                if (GState.MovingUp == aDir) id = GestureID.FlickUpWith2Finger;
            }
            else if (3 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.FlickRightWith3Finger;
                if (GState.MovingLeft == aDir) id = GestureID.FlickLeftWith3Finger;
                if (GState.MovingDown == aDir) id = GestureID.FlickDownWith3Finger;
                if (GState.MovingUp == aDir) id = GestureID.FlickUpWith3Finger;
            }
            else if (4 == aNumberOfFingers)
            {
                if (GState.MovingRight == aDir) id = GestureID.FlickRightWith4Finger;
                if (GState.MovingLeft == aDir) id = GestureID.FlickLeftWith4Finger;
                if (GState.MovingDown == aDir) id = GestureID.FlickDownWith4Finger;
                if (GState.MovingUp == aDir) id = GestureID.FlickUpWith4Finger;
            }

            createGesture(id);
        }
    }
}
