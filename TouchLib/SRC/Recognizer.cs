using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;

//using ShakeGestures;
//using System.

using Microsoft.Xna.Framework;
using AppAnalytics.ShakeGestures;

namespace AppAnalytics
{
    internal class Recognizer
    {
        #region data_types
        enum GState
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
            public GData()
            {
                state = GState.None; 
            }
            public GData(GState aState)
            {
                state = aState; 
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
        #endregion
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
                lock (_lockObject)
                {
                    t = mPrevTapOccured;
                }
                return t;
            }
        }
        #region private_memb
        private static readonly object _lockObject = new object();
        private static Recognizer mInstance;

        string mBufferPageUri = "none";
        string mBufferElementUri = "none";

        double mPreDistance = 0;
        double mPreAngle = 0;

        double[] mPreMoveX = new double[4] { 0, 0, 0, 0 };
        double[] mPreMoveY = new double[4] { 0, 0, 0, 0 };
        double[] mPreFlickX = new double[4] { 0, 0, 0, 0 };
        double[] mPreFlickY = new double[4] { 0, 0, 0, 0 };

        double mTouchStarted = 0;
        double mPositionX = 0;
        double mPositionY = 0; // where the gesture begun

        Dir mTrevDir = Dir.None;

        // about taps
        double mPrevTapOccured = 0;
        int tapsInRow = 0;

        double mTimeStamp = 0;
        int prevFingers = 0;

        GData mPrevGesture = new GData();

        double mInsensitivity = 0;
        #endregion
        // public section //////////////////////////////
        public static Recognizer Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new Recognizer()); }
        }
        protected Recognizer() 
        {
            Touch.FrameReported += new TouchFrameEventHandler(touchFrameReported);
            ShakeGesturesHelper.Instance.ShakeGesture +=
                new EventHandler<ShakeGestureEventArgs>(instanceShakeGesture);
            ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 4;
            ShakeGesturesHelper.Instance.Active = true;
        }

        void instanceShakeGesture(object sender, ShakeGestureEventArgs e)
        {
            Debug.WriteLine("shaking");
            createGesture(GestureID.Shake);
        }

        public byte Init()
        {
            mTimeStamp = convertToUnixTimestamp(DateTime.Now); 

            return 0;
        }

        public void createTapGesture (int aCountOfTaps, int aNumberOfFingers)
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

        private void createSwipeGesture( int aNumberOfFingers, Dir aDir)
        {
            GestureID id = GestureID.SwipeDownWith1Finger;

            if (1 == aNumberOfFingers)
            {
                if (Dir.Right == aDir) id = GestureID.SwipeRightWith1Finger;
                if (Dir.Left == aDir) id = GestureID.SwipeLeftWith1Finger;
                if (Dir.Down == aDir) id = GestureID.SwipeDownWith1Finger;
                if (Dir.Up == aDir) id = GestureID.SwipeUpWith1Finger;
            }
            else if (2 == aNumberOfFingers)
            {
                if (Dir.Right == aDir) id = GestureID.SwipeRightWith2Finger;
                if (Dir.Left == aDir) id = GestureID.SwipeLeftWith2Finger;
                if (Dir.Down == aDir) id = GestureID.SwipeDownWith2Finger;
                if (Dir.Up == aDir) id = GestureID.SwipeUpWith2Finger;
            }
            else if (3 == aNumberOfFingers)
            {
                if (Dir.Right == aDir) id = GestureID.SwipeRightWith3Finger;
                if (Dir.Left == aDir) id = GestureID.SwipeLeftWith3Finger;
                if (Dir.Down == aDir) id = GestureID.SwipeDownWith3Finger;
                if (Dir.Up == aDir) id = GestureID.SwipeUpWith3Finger;
            }
            else if (4 == aNumberOfFingers)
            {
                if (Dir.Right == aDir) id = GestureID.SwipeRightWith4Finger;
                if (Dir.Left == aDir) id = GestureID.SwipeLeftWith4Finger;
                if (Dir.Down == aDir) id = GestureID.SwipeDownWith4Finger;
                if (Dir.Up == aDir) id = GestureID.SwipeUpWith4Finger;
            }

            createGesture(id);
        }

        private void createHoldGesture(int aNumberOfFingers)
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

        private void createFlickGesture(int aNumberOfFingers, Dir aDir)
        {
            GestureID id = GestureID.SwipeDownWith1Finger;

            if (1 == aNumberOfFingers)
            {
                if (Dir.Right == aDir)  id = GestureID.FlickRightWith1Finger;
                if (Dir.Left == aDir)   id = GestureID.FlickLeftWith1Finger;
                if (Dir.Down == aDir)   id = GestureID.FlickDownWith1Finger;
                if (Dir.Up == aDir)     id = GestureID.FlickUpWith1Finger;
            }
            else if (2 == aNumberOfFingers)
            {
                if (Dir.Right == aDir)  id = GestureID.FlickRightWith2Finger;
                if (Dir.Left == aDir)   id = GestureID.FlickLeftWith2Finger;
                if (Dir.Down == aDir)   id = GestureID.FlickDownWith2Finger;
                if (Dir.Up == aDir)     id = GestureID.FlickUpWith2Finger;
            }
            else if (3 == aNumberOfFingers)
            {
                if (Dir.Right == aDir)  id = GestureID.FlickRightWith3Finger;
                if (Dir.Left == aDir)   id = GestureID.FlickLeftWith3Finger;
                if (Dir.Down == aDir)   id = GestureID.FlickDownWith3Finger;
                if (Dir.Up == aDir)     id = GestureID.FlickUpWith3Finger;
            }
            else if (4 == aNumberOfFingers)
            {
                if (Dir.Right == aDir)  id = GestureID.FlickRightWith4Finger;
                if (Dir.Left == aDir)   id = GestureID.FlickLeftWith4Finger;
                if (Dir.Down == aDir)   id = GestureID.FlickDownWith4Finger;
                if (Dir.Up == aDir)     id = GestureID.FlickUpWith4Finger;
            }

            createGesture(id);
        }


        public void createGesture(GestureID aID)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => getCurrentPage());
            System.Windows.Point p = new System.Windows.Point(LastPosX, LastPosY);

            GestureData gd = GestureData.create(aID, p, mBufferElementUri, mBufferPageUri);

            Detector.pushReport(gd);
        }

        private void getCurrentPage()
        {
            var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;

            var uri = currentPage.NavigationService.CurrentSource;

            lock (_lockObject)
            {
                mBufferPageUri = uri.ToString();
            }
        }

        private void getElementUri()
        {
            var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;
           // PhoneApplicationFrame g; g.con
           // currentPage.Content
            //var uri = currentPage.
            //currentPage.Children;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(currentPage); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(currentPage, i);
                
                if (child != null && child is System.Windows.Controls.Control)
                {
                    checkChild(child, currentPage);
                }
                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    subChildSearch(child);
                }
            }
        }
        
        private bool isInBBox (System.Windows.UIElement el, System.Windows.UIElement ancestor)
        {
            GeneralTransform objGeneralTransform = el.TransformToVisual(Application.Current.RootVisual as UIElement);
            var origin = el.RenderTransformOrigin;
            var rect = new Rect(origin, el.RenderSize);

            System.Windows.Point point = objGeneralTransform.Transform(new System.Windows.Point(0, 0));
            rect = objGeneralTransform.TransformBounds(rect);
            
            return (rect.Contains(new System.Windows.Point(LastPosX, LastPosY)));
        }

        private void checkChild(DependencyObject child, DependencyObject parent)
        {
            if ((child as UIElement) != null && !((child as UIElement).Visibility == Visibility.Visible))
            {
                return;
            }
            else if ((child as FrameworkElement) != null && !((child as FrameworkElement).Visibility == Visibility.Visible))
            {
                return;
            }

            if (child != null && child is System.Windows.Controls.Control)
            {
                var el = child as System.Windows.Controls.Control;
                if (isInBBox(el, parent as UIElement))
                {
                    lock (_lockObject)
                    {
                        mBufferElementUri = el.Name;
                    }
                }
            }
            else if (child != null && child is System.Windows.FrameworkElement)
            {
                var el = child as System.Windows.FrameworkElement;
                if (isInBBox(el, parent as UIElement))
                {
                    lock (_lockObject)
                    {
                        mBufferElementUri = el.Name;
                    }
                }
            }
        }

        private void subChildSearch(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                checkChild(child, parent);

                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    subChildSearch(child);
                }
            }
        }

        // TODO: code conv. 
        const double rotationThreshold = 1.5f;
        const double zoomThreshold = 0.00;
        const double holdThreshold = 0.01;

        const double swipeThreshold = 0.07;

        const double insensitivityConst = 0.08;
        const double timeForTap = 0.22f;

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
                //var x = Math.Min(resolutionX(), resolutionY());
                return zoomThreshold;
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

        double getAverageOffsets(TouchPointCollection tpc)
        {
            if ((PrevFingers * tpc.Count) == 0)
            {
                return 0;
            }

            double f = 0;
            PrevFingers = PrevFingers > 4 ? 4 : PrevFingers;
            for (int i = 0; (i < PrevFingers) && (i < tpc.Count); ++i)
            {
                double x = tpc[i].Position.X;
                double y = tpc[i].Position.Y;
                double length = Math.Sqrt(Math.Pow((x - mPreFlickX[i]), 2) +
                    Math.Pow((y - mPreFlickY[i]), 2) );

                f += length;
            }
            int numb = (Math.Min(PrevFingers, tpc.Count));

            return f / numb;
        }

        Vector2 getMoveVector(TouchPointCollection tpc)
        {
            var vec = new Vector2(0);
            if ((PrevFingers * tpc.Count) == 0)
            {
                return vec;
            }
            PrevFingers = PrevFingers > 4 ? 4 : PrevFingers;
            for (int i = 0; (i < PrevFingers) && (i < tpc.Count); ++i)
            {
                float x = (float) tpc[i].Position.X;
                float y = (float) tpc[i].Position.Y;

                float x1 = (float) mPreMoveX[i];
                float y1 = (float) mPreMoveY[i];

                vec += new Vector2(x, y) - new Vector2(x1, y1);
            }
            int numb = (Math.Min(PrevFingers, tpc.Count));

            return vec;
        }

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
        // MAIN CALLBACK //////////////////////////////////////////////////////////////////////////////////////////
        void touchFrameReported(object sender, TouchFrameEventArgs e)
        {
            // insens. processing
            // writing gesture
            double curTime = convertToUnixTimestamp(DateTime.Now);
            if (mInsensitivity > 0)
            {
                mInsensitivity -= curTime - mTimeStamp;
                mTimeStamp = curTime;
                return;
            }

            var content = ((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage;
            TouchPointCollection tpc = e.GetTouchPoints(content);
            int fingers = tpc.Count;

            handleMovement(tpc);
            updateStoredValues(tpc);
            PrevFingers = fingers;
        }
        // ------------- //////////////////////////////////////////////////////////////////////////////////////////

        Vector2 createVec(double x, double y)
        {
            return new Vector2((float)x, (float)y);
        }

        void handleChangingDirection(Dir d, TouchPointCollection tpc)
        {
            if (d == mTrevDir)
                return;

            int numb = tpc.Count > 4 ? 4 : tpc.Count;
            mPositionX = tpc[0].Position.X;
            mPositionY = tpc[0].Position.Y;

            if (d == Dir.Left)
            {
                Debug.WriteLine("<-- " + PrevFingers);
                if (isMovingState(mPrevGesture.state))
                {
                    createFlickGesture(PrevFingers, d); // submitting PREVIOUS movement
                }
                mPrevGesture.state = GState.MovingLeft;
            }
            else if (d == Dir.Right)
            {
                Debug.WriteLine("--> " + PrevFingers);
                if (isMovingState(mPrevGesture.state))
                {
                    createFlickGesture(PrevFingers, d); // submitting PREVIOUS movement
                }
                mPrevGesture.state = GState.MovingRight;
            }
            else if (d == Dir.Up)
            {
                Debug.WriteLine("/\\ " + PrevFingers);
                if (isMovingState(mPrevGesture.state))
                {
                    createFlickGesture(PrevFingers, d); // submitting PREVIOUS movement
                }
                mPrevGesture.state = GState.MovingUp;
            }
            else if (d == Dir.Down)
            {
                Debug.WriteLine("\\/ " + PrevFingers);
                if (isMovingState(mPrevGesture.state))
                {
                    createFlickGesture(PrevFingers, d); // submitting PREVIOUS movement
                }
                mPrevGesture.state = GState.MovingDown; 
            }
        }

        double startTap = 0;

        bool doesTapHappend(TouchPointCollection tpc)
        {
            //Debug.("Tap - up");
            var v1 = new Vector2((float)mPositionX, (float)mPositionY);
            var v2 = new Vector2((float)tpc[0].Position.X, (float) tpc[0].Position.Y);
            float len = ( v1 - v2 ).Length();
            if (len > (HoldThreshold * 0.6) )
            {
                if (TapsInRow > 0)
                {
                    lock (_lockObject)
                    {
                        mPrevTapOccured = 0;
                    }
                }

                return false;
            }

            double dbg = Math.Abs(getNow() - startTap);
            if (dbg < TimeForTap)
            {
                if (TapsInRow >= 2)
                {
                    Debug.WriteLine("[triple tap with ]" + PrevFingers + "fingers");
                    createTapGesture(3, PrevFingers);
                    TapsInRow = 0;
                    mPrevGesture.state = GState.None;

                    return true;
                }
                else
                {
                    lastTapFingers = tpc.Count > 4 ? 4 : tpc.Count; 

                    mPrevTapOccured = getNow();
                    TapsInRow = TapsInRow + 1;
                    Debug.WriteLine("                            tapsInRow=" + TapsInRow);
                    mPrevGesture.state = GState.None;

                    return true;
                }
            }
            return false;
        }

        bool onFingerUp(TouchPointCollection tpc)
        {
            TouchPoint flickPoint = tpc[0];
            if ( doesTapHappend(tpc) )
            {
                return true;
            }
            double nowflickX = flickPoint.Position.X;
            double nowflickY = flickPoint.Position.Y;

            double length = Math.Pow((nowflickX - mPreMoveX[0]), 2) 
                            +  Math.Pow((nowflickY - mPreMoveY[0]), 2);

            Vector2 vec = createVec(mPreMoveX[0] - flickPoint.Position.X,
                mPreMoveY[0] - flickPoint.Position.Y);

            var dir = getDirection(vec);

            if (length > SwipeThreshold && isMovingState(mPrevGesture.state))
            {
                if (Dir.Down == dir)
                {
                    Debug.WriteLine("[SWIPE] down with " + PrevFingers);
                    createSwipeGesture(PrevFingers, dir);
                }
                else if (Dir.Left == dir)
                {
                    Debug.WriteLine("[SWIPE] left with " + PrevFingers);
                    createSwipeGesture(PrevFingers, dir);
                }
                else if (Dir.Right == dir)
                {
                    Debug.WriteLine("[SWIPE] right with " + PrevFingers);
                    createSwipeGesture(PrevFingers, dir);
                }
                else if (Dir.Up == dir)
                {
                    Debug.WriteLine("[SWIPE] up with " + PrevFingers);
                    createSwipeGesture(PrevFingers, dir);
                }
            }
            else if ( isMovingState(mPrevGesture.state))
            {
                Debug.WriteLine("<movement completed>");
                createFlickGesture(PrevFingers, mTrevDir); // submitting PREVIOUS movement
                //send move event
            }

            mPrevGesture.state = GState.None;
            return false;
        }

        bool onFirstFingerMove(TouchPointCollection tpc)
        {
            TouchPoint flickPoint = tpc[0];
            double changes = getAverageOffsets(tpc);

            mPreFlickX.CopyTo(mPreMoveX, 0);
            mPreFlickY.CopyTo(mPreMoveY, 0);
            for (int i = 0; i < Math.Min(tpc.Count, 4); ++i)
            {
                mPreFlickX[i] = tpc[i].Position.X;
                mPreFlickY[i] = tpc[i].Position.Y;
            }

            double nowflickX = flickPoint.Position.X;
            double nowflickY = flickPoint.Position.Y;

            // get average moving
            if (changes < HoldThreshold && mPrevGesture.state != GState.Hold && !isRotateState(mPrevGesture.state) &&
                !isZoomState(mPrevGesture.state) && PrevFingers == tpc.Count)
            {
                if (isMovingState(mPrevGesture.state))
                {
                    return true;
                }
                if ( (getNow() - mTouchStarted) < (TimeForTap * 1.3) )
                { 
                    return false;
                }

                Debug.WriteLine("[HOLD_1] " + tpc.Count);
                createHoldGesture(tpc.Count > 4 ? 4 : tpc.Count);
                mPrevGesture.state = GState.Hold;

                return true;
            }
            else if (changes < HoldThreshold &&  PrevFingers != tpc.Count && tpc.Count <= 4)
            {
                Debug.WriteLine("[None] " + tpc.Count);
                mPrevGesture.state = GState.None;

                return true;
            }
            else if (changes > (HoldThreshold * 0.7) && PrevFingers == tpc.Count)
            {
                var move = getMoveVector(tpc); 
                var rv = new Vector2((float)resolutionX(), (float)resolutionY());
                var normalized = (move.Length() / rv.Length()) * 100;

                GState domState = chooseDominating(tpc, normalized); // rotate or scale or move point

                if (isRotateState(domState))
                {
                    handleRotate(tpc);
                }
                else if (isZoomState(domState))
                {
                    handleZoom(tpc);
                }
                else if (isMovingState(domState))
                {
                    Vector2 vec = createVec(mPreMoveX[0] - flickPoint.Position.X,
                        mPreMoveY[0] - flickPoint.Position.Y);

                    handleChangingDirection(getDirection(vec), tpc);
                    mTrevDir = getDirection(vec);
                }
            }

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
                    mPreFlickX[i] = tpc[i].Position.X;
                    mPreFlickY[i] = tpc[i].Position.Y;
                }
                //Debug.WriteLine("finger - down");
                startTap = getNow();
                mTrevDir = Dir.None;

                mPositionX = flickPoint.Position.X;
                mPositionY = flickPoint.Position.Y;

                Deployment.Current.Dispatcher.BeginInvoke(() => getElementUri());
                mTouchStarted = getNow();
            }

            return false;
        }

        void updateStoredValues(TouchPointCollection tpc)
        {
            if (tpc.Count > 1)
            {
                TouchPoint point1 = tpc[0];
                TouchPoint point2 = tpc[1];

                double X1 = point1.Position.X;
                double X2 = point2.Position.X;
                double Y1 = point1.Position.Y;
                double Y2 = point2.Position.Y;

                if ((X2 - X1) == 0)
                {
                    mPreAngle = 90;
                }
                else
                {
                    mPreAngle = Math.Atan((Y2 - Y1) / (X2 - X1));
                }

                mPreDistance = Math.Sqrt( Math.Pow((X1 - X2), 2) + Math.Pow((Y1 - Y2), 2));
            }
        }

        GState chooseDominating(TouchPointCollection tpc, double avarageMove)
        {
            if ( isRotateState(mPrevGesture.state))
            {
                return mPrevGesture.state;
            }
            else if (isZoomState(mPrevGesture.state))
            {
                return mPrevGesture.state;
            }

            GState st = GState.None;
            // check for rotate, movement or scale 
            var rm = checkForRotation(tpc);
            var zm = checkForZoom(tpc);
            
            if ( (zm > rm) && (zm > avarageMove) )
            {
                st = GState.Shrink;
            }
            else if ((rm > zm) && (rm > avarageMove))
            {
                st = GState.RotateC;
            }
            else
            {
                st = GState.MovingDown;
            }

            return st; 
        }

        bool isMovingState(GState aState)
        {
            return (aState == GState.MovingUp) || (aState == GState.MovingDown)
                || (aState == GState.MovingLeft) || (aState == GState.MovingRight);
        }

        bool isRotateState(GState aState)
        {
            return (aState == GState.RotateC) || (aState == GState.RotateAC);
        }

        bool isZoomState(GState aState)
        {
            return (aState == GState.Shrink) || (aState == GState.Enlarge);
        }

        const UInt16 kZoomMetricCf = 1;
        const UInt16 kRotateMetricCf = 2;

        double checkForZoom(TouchPointCollection tpc)
        {
            double metric = 0;
            if (tpc.Count != 2)
            {
                return 0; // we can't zoom with just one finger
            }
            else
            {
                TouchPoint point1 = tpc[0];
                TouchPoint point2 = tpc[1];

                double X1 = point1.Position.X;
                double X2 = point2.Position.X;
                double Y1 = point1.Position.Y;
                double Y2 = point2.Position.Y;

                // Detect two fingers enlargement and shrink.
                var distance = Math.Sqrt( Math.Pow((X1 - X2), 2) + Math.Pow((Y1 - Y2), 2) );
                var distDif = distance - mPreDistance ;
                distDif = Math.Abs(distDif);
                if (distDif > 0)
                {
                    var vec = new Vector2( (float) resolutionX(), (float) resolutionY() );
                    distDif = (distDif / vec.Length() ) * 100; // percent

                    metric = kZoomMetricCf * distDif;
                }
                //preDistance = distance;
            }

            return metric;
        }

        double checkForRotation(TouchPointCollection tpc)
        {
            double metric = 0;
            if (tpc.Count != 2)
            {
                return 0; 
            }
            else
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

                double diff = 0;
                if ( Math.Sign(nowAngle) == Math.Sign(mPreAngle) )
                {
                    diff = Math.Abs(nowAngle - mPreAngle);
                }
                else
                {
                    double na = nowAngle, pa = mPreAngle;

                    if (mPreAngle < 0) pa = (Math.PI * 2) + pa;
                    if (nowAngle < 0) na = (Math.PI * 2) + na;

                    diff = Math.Abs(na - pa);
                }

                if (diff > Math.PI)
                    diff = diff - Math.PI;
                //preAngle = nowAngle;
                metric =  ( diff / (Math.PI*2) )* 100 * kRotateMetricCf; // 100 -> percent
            }

            return metric;
        }

        bool handleZoom(TouchPointCollection tpc)
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
            var distance = Math.Sqrt( Math.Pow((X1 - X2), 2) + Math.Pow((Y1 - Y2), 2) );
            if ( (distance > (mPreDistance)) && (GState.Enlarge != mPrevGesture.state) )
            {
                Debug.WriteLine("enlarge");
                // push
                createGesture(GestureID.ZoomWith2Finger);
                mPrevGesture.state = GState.Enlarge; // also known as zoom
                flag = true;
            }
            else if ( (distance < (mPreDistance)) && (GState.Shrink != mPrevGesture.state))
            {
                Debug.WriteLine("shrink"); // also known as pinch
                // push
                createGesture(GestureID.PinchWith2Finger);
                mPrevGesture.state = GState.Shrink;
                flag = true;
            }
            mPreDistance = distance;

            return flag;
        }

        bool handleRotate(TouchPointCollection tpc)
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

//                 if (Math.Abs( Math.Abs(preAngle) - Math.Abs(nowAngle) ) < rotationThreshold)
//                 {
//                     Debug.WriteLine("...");
//                     preAngle = nowAngle;
//                     return false;
//                 }
//                 else
                if ((GState.RotateC != mPrevGesture.state) && (GState.RotateAC != mPrevGesture.state))
                {
                    Debug.WriteLine("rotation");
                    createGesture(GestureID.RotateWith2Finger);
                    mPrevGesture.state = GState.RotateAC;
                }

//                 if ((nowAngle > preAngle) && (GState.RotateC != prevGesture.state))
//                 {
//                     Debug.WriteLine("clock wise rotation");
//                     prevGesture.state = GState.RotateC;
//                     preAngle = nowAngle;
//                     return true;
//                 }
//                 else if (GState.RotateAC != prevGesture.state)
//                 {
//                     Debug.WriteLine("counter clock wise rotation");
//                     prevGesture.state = GState.RotateAC;
//                     preAngle = nowAngle;
//                     return true;
//                 }
            }
            return false;
        }


        public int PrevFingers 
        {
            get
            {
                int a = 0;
                lock (_lockObject)
                {
                    a = prevFingers;
                }
                return a;
            }
            set
            {
                lock (_lockObject)
                {
                    prevFingers = value;
                }
            }
        }

    }
}
