using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchLib
{
    internal class GestureData
    {
        private UInt64 mIndex = 0;
        public byte[] Index
        {
            get { return BitConverter.GetBytes(mIndex); }
        }

        public byte ActionID = 0;

        private UInt64 mTime = 0;
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
        { get { return BitConverter.GetBytes(mPosY); } }

        public byte[] Param1
        { get { return BitConverter.GetBytes(0); } }
    }
}
