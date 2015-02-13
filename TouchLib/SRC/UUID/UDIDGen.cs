using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchLib.UUID
{
    internal class UDIDGen
    {
        private static UDIDGen mInstance;

        private Guid mGUID;
        private Guid mSessionID;

        protected UDIDGen()
        {
            mGUID = new Guid();
            mSessionID = Guid.NewGuid();

            //TODO : check if exist on local machine
            if ( this.existOnDevice() )
            {
                mGUID = Guid.NewGuid();
            }
        }

        private bool existOnDevice()
        {
            return true;
        }

        // public section //////////////////////////////
        public static UDIDGen Instance
        {
            get { return mInstance ?? (mInstance = new UDIDGen()); }
        }

        public byte[] SessionID
        {
            get 
            {
                return mSessionID.ToByteArray();
            }
        }

        public byte[] UDID
        {
            get
            {
                return mGUID.ToByteArray();
            }
        }
    }
}
