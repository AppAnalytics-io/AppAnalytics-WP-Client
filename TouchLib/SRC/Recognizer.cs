using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using System.Windows.Input;
using System.Diagnostics;

using Microsoft.Xna.Framework;

namespace TouchLib
{
    internal class Recognizer
    {
        enum GState
        {
            MovingUp = 0,
            MovingDown,
            MovingLeft,
            MovingRight,

            FastMovingUp,
            FastMovingDown,
            FastMovingLeft,
            FastMovingRight,

            Enlarge,
            Shrink,

            RotateC,
            RotateAC,

            Hold,

            FingersUp,
            FingerDown,

            None
        }

        enum Dir
        {
            Up = 0,
            Down,
            Left,
            Right,
            None = -1
        }

        class GData
        {
            public GState state;
            public  int fingers;
            public GData()
            {
                state = GState.None;
                fingers = 1;
            }
            public GData(int count, GState aState)
            {
                state = aState;
                fingers = count;
            }
        }

        Dir getDirection( Vector2 vec )
        {
            if ( Math.Abs(vec.X) > Math.Abs(vec.Y) )
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

        private double convertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (diff.TotalSeconds);
        }

        static public double getNow()
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = DateTime.Now.ToUniversalTime() - origin;
            return (diff.TotalSeconds);
        }

        public double PrevTapOccured
        {
            get
            {
                double t = 0;
                lock (prevGesture)
                {
                    t = prevTapOccured;
                }
                return t;
            }
        }

        private static Recognizer mInstance;
        // public section //////////////////////////////
        public static Recognizer Instance
        {
            get { return mInstance ?? (mInstance = new Recognizer()); }
        }
        protected Recognizer() 
        {
            Touch.FrameReported += new TouchFrameEventHandler(touchFrameReported);
        }

        public byte Init()
        {
            timeStamp = convertToUnixTimestamp(DateTime.Now);
            return 0;
        }
        // TODO: code conv. 
        const double rotationThreshold = 5.0f;
        const double zoomThreshold = 0.04;
        const double holdThreshold = 0.03;

        const double swipeThreshold = 0.07;

        const double insensitivityConst = 0.2;
        const double timeForTap = 0.1f;

        double HoldThreshold
        {
            get
            {
                var x = Math.Min(resolutionX(), resolutionY());
                return x * holdThreshold;
            }
        }
        double SwipeThreshold
        {
            get
            {
                var x = Math.Min(resolutionX(), resolutionY());
                return x * swipeThreshold;
            }
        }
        double ZoomThreshold
        {
            get
            {
                var x = Math.Min(resolutionX(), resolutionY());
                return x * zoomThreshold;
            }
        }

        public int TapsInRow
        {
            get
            {
                int a = 0;
                lock (prevGesture)
                {
                    a = tapsInRow;
                }
                return a;
            }
            set
            {
                lock (prevGesture)
                {
                    tapsInRow = value;
                }
            }
        }

        // about flicks
        double preDistance = 0;
        double preAngle = 0;

        double[] preMoveX = new double[4] { 0, 0, 0, 0 };
        double[] preMoveY = new double[4] { 0, 0, 0, 0 };
        double[] preFlickX = new double[4] { 0, 0, 0, 0 };
        double[] preFlickY = new double[4] { 0, 0, 0, 0 };

        double getAverageFrameLen(TouchPointCollection tpc)
        {
            if ( (PrevFingers * tpc.Count) == 0)
            {
                return 0;
            }

            double f = 0;
            PrevFingers = PrevFingers > 4 ? 4 : PrevFingers;
            for (int i = 0; (i < PrevFingers) && (i < tpc.Count); ++i)
            {
                double x = tpc[i].Position.X;
                double y = tpc[i].Position.Y;
                double length = Math.Pow((x - preFlickX[i]), 2) +
                    Math.Pow((y - preFlickY[i]), 2);

                f += length;
            }
            int numb = (Math.Min(PrevFingers, tpc.Count));

            return f / numb;
        }

        Dir prevDir = Dir.None;

        // about taps
        double prevTapOccured = 0;
        int tapsInRow = 0;

        double timeStamp = 0;
        int prevFingers = 0;

        //Queue<GData> stateSeq = new Queue<GData>();
        GData prevGesture = new GData();

        double insensitivity = 0;

        private static double resolutionX()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            //double h = (int)Math.Ceiling(content.ActualHeight * scale);
            double w = (int)Math.Ceiling(content.ActualWidth * scale);

            return w;
        }

        private static double resolutionY()
        {
            var content = Application.Current.Host.Content;
            double scale = (double)content.ScaleFactor / 100;

            double h = (int)Math.Ceiling(content.ActualHeight * scale);
            //double w = (int)Math.Ceiling(content.ActualWidth * scale);

            return h;
        }

        void touchFrameReported(object sender, TouchFrameEventArgs e)
        {
            // insens. processing
            // writing gesture
            double curTime = convertToUnixTimestamp(DateTime.Now);
            if (insensitivity > 0)
            {
                insensitivity -= curTime - timeStamp;
                return;
            }

            var content = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;
            TouchPointCollection tpc = e.GetTouchPoints(content);
            int fingers = tpc.Count;

            handleTaps(tpc);
            PrevFingers = fingers;
        }

        Vector2 createVec(double x, double y)
        {
            return new Vector2((float)x, (float)y);
        }

        void handleChangingDirection(Dir d)
        {
            if (d == prevDir)
               return;

            if (d == Dir.Left)
            {
                Debug.WriteLine("<--");
                prevGesture.state = GState.MovingLeft;
            }
            else if (d == Dir.Right)
            {
                Debug.WriteLine("-->");
                prevGesture.state = GState.MovingRight;
            }
            else if (d == Dir.Up)
            {
                Debug.WriteLine("//\\up");
                prevGesture.state = GState.MovingUp;
            }
            else if (d == Dir.Down)
            {
                Debug.WriteLine("\\//down");
                prevGesture.state = GState.MovingDown;
            }
        }

        double startTap = 0;
        bool handleTaps(TouchPointCollection tpc)
        {
            TouchPoint flickPoint = tpc[0];
            // do it in detector
//             double tstDif = getNow() - prevTapOccured;
//             if ((tstDif > timeForTap) && (TapsInRow > 0))
//             {
//                 Debug.WriteLine(TapsInRow + " [taps]");
//                 prevTapOccured = 0;
//                 TapsInRow = 0;
//             }
            ////////////////////

            if (flickPoint.Action == TouchAction.Up)
            {
                //Debug.("Tap - up");
                double dbg = Math.Abs(getNow() - startTap);
                if ( dbg < timeForTap)
                {
                    if ( (PrevFingers != tpc.Count) && (TapsInRow > 0) )
                    {
                        Debug.WriteLine(TapsInRow + "[ tap with ]" + PrevFingers + "fingers");
                        TapsInRow = 0;
                        return false;
                    }

                    if (TapsInRow >= 2)
                    {
                        Debug.WriteLine("[triple tap with ]" + PrevFingers + "fingers"  );
                        TapsInRow = 0;
                        return true;
                    }
                    else
                    {
                        prevTapOccured = getNow();
                        TapsInRow = TapsInRow + 1;
                        Debug.WriteLine("                            tapsInRow=" + TapsInRow);
                        return true;
                    }
                }
//                 else
//                 {
//                     if (TapsInRow > 0)
//                     {
//                         Debug.WriteLine(TapsInRow + " [taps <> with]" + PrevFingers + "fingers");
//                         // emite !
//                     }
//                     TapsInRow = 0;
//                 }

                double nowflickX = flickPoint.Position.X;
                double nowflickY = flickPoint.Position.Y;
                double length = Math.Pow((nowflickX - preMoveX[0]), 2) +
                    Math.Pow((nowflickY - preMoveY[0]), 2);

                Vector2 vec = createVec(preMoveX[0] - flickPoint.Position.X,
                    preMoveY[0] - flickPoint.Position.Y);

                var dir = getDirection(vec);

                if (length > SwipeThreshold)
                {
                    if (Dir.Down == dir)
                    {
                        Debug.WriteLine("[SWIPE] down");
                    }
                    else if (Dir.Left == dir)
                    {
                        Debug.WriteLine("[SWIPE] left");
                    }
                    else if (Dir.Right == dir)
                    {
                        Debug.WriteLine("[SWIPE] right");
                    }
                    else if (Dir.Up == dir)
                    {
                        Debug.WriteLine("[SWIPE] up");
                    }
                }
                else if (length > HoldThreshold)
                {
                    if (Dir.Down == dir )
                    {
                        Debug.WriteLine("<<[flick] down");
                    }
                    else if (Dir.Left == dir)
                    {
                        Debug.WriteLine("<<[flick] left");
                    }
                    else if (Dir.Right == dir)
                    {
                        Debug.WriteLine("<<[flick] right");
                    }
                    else if (Dir.Up == dir)
                    {
                        Debug.WriteLine("<<[flick] up");
                    }
                }
                prevGesture.state = GState.None;
            }
            else if (flickPoint.Action == TouchAction.Move)
            {
                double changes = getAverageFrameLen(tpc);

                preFlickX.CopyTo(preMoveX, 0);
                preFlickY.CopyTo(preMoveY, 0);
                for (int i = 0; i < Math.Min(tpc.Count, 4); ++i )
                {
                    preFlickX[i] = tpc[i].Position.X;
                    preFlickY[i] = tpc[i].Position.Y;
                }

                double nowflickX = flickPoint.Position.X;
                double nowflickY = flickPoint.Position.Y;

                // get average moving
                if (changes < HoldThreshold && prevGesture.state != GState.Hold)
                {
                    Debug.WriteLine("[HOLD] " + tpc.Count);
                    prevGesture = new GData(tpc.Count, GState.Hold);

                    return true;
                }
                else if (changes < HoldThreshold && PrevFingers != tpc.Count)
                {
                    Debug.WriteLine("[HOLD] " + tpc.Count);
                    prevGesture = new GData(tpc.Count, GState.Hold);

                    return true;
                }
                else if (changes > (HoldThreshold*0.7))
                {

                    if ( !checkForZoom(tpc) && !checkForRotate(tpc))
                    {
                        Vector2 vec = createVec(preMoveX[0] - flickPoint.Position.X,
                            preMoveY[0] - flickPoint.Position.Y);

                        handleChangingDirection(getDirection(vec));
                        prevDir = getDirection(vec);
                    }
                }
            }
            else if (flickPoint.Action == TouchAction.Down)
            {
                for (int i = 0; i < Math.Min(tpc.Count, 4); ++i)
                {
                    preFlickX[i] = tpc[i].Position.X;
                    preFlickY[i] = tpc[i].Position.Y;
                }
                Debug.WriteLine("finger - down");
                startTap = getNow();
                prevDir = Dir.None;
            }

            return false;
        }

        bool checkForZoom(TouchPointCollection tpc)
        {
            if (tpc.Count < 2) return false;

            bool flag = false;

            TouchPoint point1 = tpc[0];
            TouchPoint point2 = tpc[1];

            double X1 = point1.Position.X;
            double X2 = point2.Position.X;
            double Y1 = point1.Position.Y;
            double Y2 = point2.Position.Y;

            // Detect two fingers enlargement and shrink.
            var distance = Math.Pow((X1 - X2), 2) + Math.Pow((Y1 - Y2), 2);
            if ( (distance > (preDistance + ZoomThreshold)) && (GState.Enlarge != prevGesture.state) )
            {
                Debug.WriteLine("enlarge");
                // push
                prevGesture.state = GState.Enlarge;
                flag = true;
            }
            else if ( (distance < (preDistance + ZoomThreshold)) && (GState.Shrink != prevGesture.state))
            {
                Debug.WriteLine("shrink");
                // push
                prevGesture.state = GState.Shrink;
                flag = true;
            }
            preDistance = distance;

            return flag;
        }

        bool checkForRotate(TouchPointCollection tpc)
        {
            if (tpc.Count >= 2)
            {
                TouchPoint point1 = tpc[0];
                TouchPoint point2 = tpc[1];

                double X1 = point1.Position.X;
                double X2 = point2.Position.X;
                double Y1 = point1.Position.Y;
                double Y2 = point2.Position.Y;
                // Detect rotation.

                double nowAngle = 0;
                if ((X2 - X1) == 0)
                {
                    nowAngle = 90;
                }
                else
                {
                    nowAngle = Math.Atan((Y2 - Y1) / (X2 - X1));
                }

                if (Math.Abs( Math.Abs(preAngle) - Math.Abs(nowAngle) ) < rotationThreshold)
                {
                    Debug.WriteLine(" <<<<threshold>>>>");
                    preAngle = nowAngle;
                    return false;
                }
                else if ((nowAngle > preAngle) && (GState.RotateC != prevGesture.state))
                {
                    Debug.WriteLine("clock wise rotation");
                    prevGesture.state = GState.RotateC;
                    return true;
                }
                else if (GState.RotateAC != prevGesture.state)
                {
                    Debug.WriteLine("counter clock wise rotation");
                    prevGesture.state = GState.RotateAC;
                    return true;
                }
            }
            return false;
        }


        public int PrevFingers 
        {
            get
            {
                int a = 0;
                lock (prevGesture)
                {
                    a = prevFingers;
                }
                return a;
            }
            set
            {
                lock (prevGesture)
                {
                    prevFingers = value;
                }
            }
        }
    }
}
