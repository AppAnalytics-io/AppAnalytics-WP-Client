using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace AppAnalytics
{
    public class GestureData
    {
        private static UInt64 mGlobalIndex = 0;
        public UInt64 mActionOrder = 0;

        public void setIndex()
        {
            mActionOrder = mGlobalIndex;
            mGlobalIndex++;
        }

        GestureData() { }

        static public GestureData create(GestureID aID, Windows.Foundation.Point aLocation, string aElement, string aPage, byte[] param1 = null)
        {
            GestureData newOne = new GestureData();
            newOne.ActionID = (byte)aID;
             
            newOne.ViewID = Encoding.UTF8.GetBytes(aPage);// Detector.getBytes(aPage); 
            newOne.ElementID = Encoding.UTF8.GetBytes(aElement);//Detector.getBytes(aElement);

            newOne.mPosX = aLocation.X;
            newOne.mPosY = aLocation.Y;

            if (param1 != null && param1.Length == 4)
            {
                newOne.mParam1 = param1;
            }

            newOne.setCurrentTime();
            newOne.setIndex();

            Detector.logSampleDbg(newOne);
            return newOne;
        }

        public byte[] ActionOrder
        {
            get { return BitConverter.GetBytes(mActionOrder); }
        }

        public byte ActionID = 0;
        //private string

        private long mTime = 0;
        public DateTime mTimeObject = new DateTime(); //mb I should use it  instead of mTime.

        public static long convertToUnixTimestamp(DateTime date)
        {
            return date.ToBinary();
        }

        public void setCurrentTime()
        {
            mTimeObject = DateTime.Now;
            mTime = convertToUnixTimestamp(DateTime.Now);
        }

        public byte[] ActionTime
        {
            get { return BitConverter.GetBytes(mTime); }
        }
        // note : mb it is better to return a string instead?

        public double mPosX = 0;
        public byte[] PosX
        { get { return BitConverter.GetBytes(mPosX); } }
        
        public double mPosY = 0;
        public byte[] PosY
        { 
            get { return BitConverter.GetBytes(mPosY); }
        }
        public void setPosY(double Y) { mPosY = Y; }


        private byte[] mParam1 = new byte[4]{0,0,0,0};
        public byte[] Param1
        { get { return mParam1; } }
        public Int32 Param1asInt32
        { get { return BitConverter.ToInt32(mParam1, 0); } }

        // View ID
        
        public byte[] ViewID = null;

        public UInt16 ViewIDLenght
        {
            get { return (UInt16)(ViewID.Length ); }
        }

        public byte[] ElementID = null;
        public UInt16 ElementIDLenght
        {
            get { return (UInt16)(ElementID.Length ); }
        }

        public string typeToString()
        {
            GestureID id = (GestureID)ActionID;
            switch (id)
            {
                case GestureID.SingleTapWith1Finger:
                    return "SingleTapWith1Finger";
                case GestureID.DoubleTapWith1Finger:
                    return "DoubleTapWith1Finger";
                case GestureID.TripleTapWith1Finger:
                    return "TripleTapWith1Finger";
                case GestureID.SingleTapWith2Finger:
                    return "SingleTapWith2Finger";
                case GestureID.DoubleTapWith2Finger:
                    return "DoubleTapWith2Finger";
                case GestureID.TripleTapWith2Finger:
                    return "TripleTapWith2Finger";
                case GestureID.SingleTapWith3Finger:
                    return "SingleTapWith3Finger";
                case GestureID.DoubleTapWith3Finger:
                    return "DoubleTapWith3Finger";
                case GestureID.TripleTapWith3Finger:
                    return "TripleTapWith3Finger";
                case GestureID.SingleTapWith4Finger:
                    return "SingleTapWith4Finger";
                case GestureID.DoubleTapWith4Finger:
                    return "DoubleTapWith4Finger";
                case GestureID.TripleTapWith4Finger:
                    return "TripleTapWith4Finger";
                case GestureID.HoldWith1Finger:
                    return "HoldWith1Finger";
                case GestureID.HoldWith2Finger:
                    return "HoldWith2Finger";
                case GestureID.HoldWith3Finger:
                    return "HoldWith3Finger";
                case GestureID.HoldWith4Finger:
                    return "HoldWith4Finger";
                case GestureID.PinchWith2Finger:
                    return "PinchWith2Finger";
                case GestureID.ZoomWith2Finger:
                    return "ZoomWith2Finger";
                case GestureID.RotateWith2Finger:
                    return "RotateWith2Finger";
                case GestureID.SwipeRightWith1Finger:
                    return "SwipeRightWith1Finger";
                case GestureID.SwipeLeftWith1Finger:
                    return "SwipeLeftWith1Finger";
                case GestureID.SwipeDownWith1Finger:
                    return "SwipeDownWith1Finger";
                case GestureID.SwipeUpWith1Finger:
                    return "SwipeUpWith1Finger";
                case GestureID.FlickRightWith1Finger:
                    return "FlickRightWith1Finger";
                case GestureID.FlickLeftWith1Finger:
                    return "FlickLeftWith1Finger";
                case GestureID.FlickDownWith1Finger:
                    return "FlickDownWith1Finger";
                case GestureID.FlickUpWith1Finger:
                    return "FlickUpWith1Finger";
                case GestureID.SwipeRightWith2Finger:
                    return "SwipeRightWith2Finger";
                case GestureID.SwipeLeftWith2Finger:
                    return "SwipeLeftWith2Finger";
                case GestureID.SwipeDownWith2Finger:
                    return "SwipeDownWith2Finger";
                case GestureID.SwipeUpWith2Finger:
                    return "SwipeUpWith2Finger";
                case GestureID.FlickRightWith2Finger:
                    return "FlickRightWith2Finger";
                case GestureID.FlickLeftWith2Finger:
                    return "FlickLeftWith2Finger";
                case GestureID.FlickDownWith2Finger:
                    return "FlickDownWith2Finger";
                case GestureID.FlickUpWith2Finger:
                    return "FlickUpWith2Finger";
                case GestureID.SwipeRightWith3Finger:
                    return "SwipeRightWith3Finger";
                case GestureID.SwipeLeftWith3Finger:
                    return "SwipeLeftWith3Finger";
                case GestureID.SwipeDownWith3Finger:
                    return "SwipeDownWith3Finger";
                case GestureID.SwipeUpWith3Finger:
                    return "SwipeUpWith3Finger";
                case GestureID.FlickRightWith3Finger:
                    return "FlickRightWith3Finger";
                case GestureID.FlickLeftWith3Finger:
                    return "FlickLeftWith3Finger";
                case GestureID.FlickDownWith3Finger:
                    return "FlickDownWith3Finger";
                case GestureID.FlickUpWith3Finger:
                    return "FlickUpWith3Finger";
                case GestureID.SwipeRightWith4Finger:
                    return "SwipeRightWith4Finger";
                case GestureID.SwipeLeftWith4Finger:
                    return "SwipeLeftWith4Finger";
                case GestureID.SwipeDownWith4Finger:
                    return "SwipeDownWith4Finger";
                case GestureID.SwipeUpWith4Finger:
                    return "SwipeUpWith4Finger";
                case GestureID.FlickLeftWith4Finger:
                    return "FlickLeftWith4Finger";
                case GestureID.FlickRightWith4Finger:
                    return "FlickRightWith4Finger";
                case GestureID.FlickDownWith4Finger:
                    return "FlickDownWith4Finger";
                case GestureID.FlickUpWith4Finger:
                    return "FlickUpWith4Finger";
                case GestureID.Shake:
                    return "Shake";
                case GestureID.Navigation:
                    return "Navigation";
                default:
                    return "default";
            }
        }
    }
}
