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
        private UInt64 mActionOrder = 0;

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

            return newOne;
        }

        public byte[] ActionOrder
        {
            get { return BitConverter.GetBytes(mActionOrder); }
        }

        public byte ActionID = 0;
        //private string

        private long mTime = 0;

        public static long convertToUnixTimestamp(DateTime date)
        {
            return date.ToBinary();
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


        private byte[] mParam1 = new byte[4]{0,0,0,0};
        public byte[] Param1
        { get { return mParam1; } }

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
    }
}
