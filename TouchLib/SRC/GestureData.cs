using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchLib
{
    internal class GestureData
    {
        private static UInt64 mGlobalIndex = 0;
        private UInt64 mActionOrder = 0;

        public void setIndex()
        {
            mActionOrder = mGlobalIndex;
            mGlobalIndex++;
        }

        static public GestureData create(GestureID aID,  Point aLocation, string aElement, string aPage)
        {
            GestureData newOne = new GestureData();
            newOne.ActionID = (byte)aID;

            newOne.ViewID = Detector.getBytes(aPage);
            newOne.ElementID = Detector.getBytes(aElement);

            newOne.mPosX = aLocation.X;
            newOne.mPosY = aLocation.Y;

            newOne.setCurrentTime();
            newOne.setIndex();

            return newOne;
        }

        public byte[] ActionOrder
        {
            get { return BitConverter.GetBytes(mActionOrder); }
        }

        public byte ActionID = 0;
        //private string

        private double mTime = 0;

        public static double convertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public void setCurrentTime()
        {
            mTime = convertToUnixTimestamp(DateTime.Now);
        }

        public byte[] ActionTime
        {
            get { return BitConverter.GetBytes(mTime); }
        }
        // note : mb it is better to return a string instead?

        private double mPosX = 0;
        public byte[] PosX
        { get { return BitConverter.GetBytes(mPosX); } }
        
        private double mPosY = 0;
        public byte[] PosY
        { 
            get { return BitConverter.GetBytes(mPosY); }
        }
        public void setPosY(double Y) { mPosY = Y; }


        public byte[] Param1
        { get { return BitConverter.GetBytes( (UInt32)0); } }

        // View ID

        public byte[] ViewID = null;

        public UInt16 ViewIDLenght
        {
            get { return (UInt16)(ViewID.Length * sizeof(char)); }
        }

        public byte[] ElementID = null;
        public UInt16 ElementIDLenght
        {
            get { return (UInt16)(ElementID.Length * sizeof(char)); }
        }
    }
}
