using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using System.Windows.Input;
using System.Diagnostics; 
 
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

using TouchPointCollection = System.Collections.ObjectModel.Collection<AppAnalytics.TouchPoint>;

/*
 * Used for detecting multiple fingers tap.
 * Will be refactored.
 */

namespace AppAnalytics
{
    internal class FrameProcessor
    {
        #region data_types 
        enum Dir
        {
            Up = 0,
            Down,
            Left,
            Right,
            None = -1
        }
         

        Dir getDirection(Vector2 vec)
        {
            if (Math.Abs(vec.X) > Math.Abs(vec.Y))
            {
                if (vec.X > 0)
                {
                    return Dir.Left;
                }
                else
                {
                    return Dir.Right;
                }
            }
            else
            {
                if (vec.Y > 0)
                {
                    return Dir.Up;
                }
                else
                {
                    return Dir.Down;
                }
            }
        }
        #endregion
        private double convertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (diff.TotalSeconds);
        }

        static public DateTime getNow()
        { 
            return DateTime.Now.ToUniversalTime();
        }

        public DateTime PrevTapOccured
        {
            get
            {
                DateTime t;
                lock (_lockObject)
                {
                    t = new DateTime(mPrevTapOccured.Ticks);
                }
                return t;
            }
            set
            {
                lock (_lockObject) { mPrevTapOccured = value; }
            }
        }
        #region private_memb
        private static readonly object _lockObject = new object();
        private static FrameProcessor mInstance;

        double mPreDistance = 0;
        double mPreAngle = 0;

        double[] mPreMoveX = new double[4] { 0, 0, 0, 0 };
        double[] mPreMoveY = new double[4] { 0, 0, 0, 0 };
        double[] mPreFlickX = new double[4] { 0, 0, 0, 0 };
        double[] mPreFlickY = new double[4] { 0, 0, 0, 0 };

        DateTime mTouchStarted = new DateTime();
        double mPositionX = 0;
        double mPositionY = 0; // where the gesture begun

        Dir mTrevDir = Dir.None;

        // about taps
        DateTime mPrevTapOccured;
        int tapsInRow = 0;

        double mTimeStamp = 0;
        int prevFingers = 0; 

        double mInsensitivity = 0;
        #endregion
        // public section //////////////////////////////
        public static FrameProcessor Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new FrameProcessor()); }
        }

        protected FrameProcessor()
        {
        }

        //const double swipeThreshold = 0.07;

        //const double insensitivityConst = 0.08;
        const double timeForTap = 0.30f;

        int lastTapFingers = 0;
        public int LastTapFingers
        {
            get
            {
                int a = 0;
                lock (_lockObject)
                {
                    a = lastTapFingers;
                }
                return a;
            }
            set
            {
                lock (_lockObject)
                {
                    lastTapFingers = value;
                }
            }
        }

        public double TimeForTap { get { return timeForTap; } }
        double HoldThreshold
        {
            get
            {
                var x = Math.Min(resolutionX(), resolutionY());
                return x * 0.06;
            }
        }

        public int TapsInRow
        {
            get
            {
                int a = 0;
                lock (_lockObject)
                {
                    a = tapsInRow;
                }
                return a;
            }
            set
            {
                lock (_lockObject)
                {
                    tapsInRow = value;
                }
            }
        }

        public double LastPosX
        {
            get
            {
                lock (_lockObject)
                {
                    return mPositionX;
                }
            }
        }
        public double LastPosY
        {
            get
            {
                lock (_lockObject)
                {
                    return mPositionY;
                }
            }
        }

        private static double resolutionX()
        {
            return Detector.getResolutionXDouble();
        }

        private static double resolutionY()
        {
            return Detector.getResolutionYDouble();
        }

        int PrevFingers = 0;
        int mMaxFingersInTap = 0;

        // MAIN CALLBACK //////////////////////////////////////////////////////////////////////////////////////////
        public void manipulationFrame(TouchPointCollection tpc)
        {
            double curTime = convertToUnixTimestamp(DateTime.Now);
            if (mInsensitivity > 0)
            {
                mInsensitivity -= curTime - mTimeStamp;
                mTimeStamp = curTime;
                return;
            }
            
            int fingers = tpc.Count > 4 ? 4 : tpc.Count;
            mMaxFingersInTap = mMaxFingersInTap < fingers ? fingers : mMaxFingersInTap;

            handleMovement(tpc);
            updateStoredValues(tpc);
        }
        // ------------- //////////////////////////////////////////////////////////////////////////////////////////

        Vector2 createVec(double x, double y)
        {
            return new Vector2((float)x, (float)y);
        }

        DateTime startTap = new DateTime();

        bool doesTapHappend(TouchPointCollection tpc)
        {
            //Debug.WriteLine("Tap - up");
            var v1 = new Vector2((float)mPositionX, (float)mPositionY);
            var v2 = new Vector2((float)tpc[0].X, (float)tpc[0].Y);
            float len = (v1 - v2).Length();
            if (len > (HoldThreshold * 0.6))
            {
                if (TapsInRow > 0)
                {
                    lock (_lockObject)
                    {
                        mPrevTapOccured = new DateTime(0);
                    }
                }

                return false;
            }
            var now = getNow();
            //Debug.WriteLine("check " + now.Ticks);
            double dbg = Math.Abs( (now - startTap).TotalSeconds );

            if (dbg < TimeForTap)
            {
                if (TapsInRow >= 2)
                {
                    Debug.WriteLine("[triple tap with ]" + PrevFingers + "fingers");
                    GestureProcessor.createTapGesture(3, PrevFingers);
                    TapsInRow = 0;
                    //mPrevGesture.state = GState.None;

                    return true;
                }
                else
                {
                    lastTapFingers = tpc.Count > 4 ? 4 : tpc.Count;
                    
                    PrevTapOccured = getNow();

                    TapsInRow = TapsInRow + 1;
                    Debug.WriteLine("                            tapsInRow=" + TapsInRow);
                    //mPrevGesture.state = GState.None;

                    return true;
                }
            }
            else
            {
                PrevTapOccured = new DateTime(0);
                TapsInRow = 0;
            }
            return false;
        }

        bool onFingerUp(TouchPointCollection tpc)
        {
            TouchPoint flickPoint = tpc[0];
            if (doesTapHappend(tpc))
            {
                return true;
            }
            if (tpc.Count == 1)
            {
                PrevFingers = mMaxFingersInTap;
                mMaxFingersInTap = 0;
            }
            return false;
        }

        bool onFirstFingerMove(TouchPointCollection tpc)
        {
            TouchPoint flickPoint = tpc[0];
           // double changes = getAverageOffsets(tpc);

            mPreFlickX.CopyTo(mPreMoveX, 0);
            mPreFlickY.CopyTo(mPreMoveY, 0);
            for (int i = 0; i < Math.Min(tpc.Count, 4); ++i)
            {
                mPreFlickX[i] = tpc[i].X;
                mPreFlickY[i] = tpc[i].Y;
            }

            double nowflickX = flickPoint.X;
            double nowflickY = flickPoint.Y;

            return false;
        }

        bool handleMovement(TouchPointCollection tpc)
        {
            TouchPoint flickPoint = tpc[0];
            bool isAnyFingerUp = false;

            foreach (var t in tpc) { isAnyFingerUp = isAnyFingerUp || (t.Action == TouchAction.Up); }

            if (flickPoint.Action == TouchAction.Up)
            {
                return onFingerUp(tpc);
            }
            else if (flickPoint.Action == TouchAction.Move)
            {
                return onFirstFingerMove(tpc);
            }
            else if (flickPoint.Action == TouchAction.Down)
            {
                for (int i = 0; i < Math.Min(tpc.Count, 4); ++i)
                {
                    mPreFlickX[i] = tpc[i].X;
                    mPreFlickY[i] = tpc[i].Y;
                }
                startTap = getNow();
                //wDebug.WriteLine("now " + startTap.Ticks);
                mTrevDir = Dir.None;

                mPositionX = flickPoint.X;
                mPositionY = flickPoint.Y;

                mTouchStarted = getNow();
            }

            return false;
        }

        void updateStoredValues(TouchPointCollection tpc)
        {
            if (tpc.Count > 1)
            {
                Point point1 = new Point(tpc[0].X, tpc[0].Y);
                Point point2 = new Point(tpc[1].X, tpc[1].Y);

                double X1 = point1.X;
                double X2 = point2.X;
                double Y1 = point1.Y;
                double Y2 = point2.Y;

                if ((X2 - X1) == 0)
                {
                    mPreAngle = 90;
                }
                else
                {
                    mPreAngle = Math.Atan((Y2 - Y1) / (X2 - X1));
                }

                mPreDistance = Math.Sqrt(Math.Pow((X1 - X2), 2) + Math.Pow((Y1 - Y2), 2));
            }
        }
    }
}