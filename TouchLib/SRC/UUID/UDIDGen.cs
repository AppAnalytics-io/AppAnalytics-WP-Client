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

        public String UUIDV4
        {
            get { return mSessionID.ToString();  }
        }

        public String ViewID = "";
        public UInt16 ViewIDLenght
        {
            get { return (UInt16)ViewID.Length; }
        }

        public String ElementID = "";
        public UInt16 ElementIDLenght
        {
            get { return (UInt16)ElementID.Length; }
        }
    }
}
