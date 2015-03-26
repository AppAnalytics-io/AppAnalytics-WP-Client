using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;

using System.Diagnostics;

namespace AppAnalytics.UUID
{
    internal class UDIDGen
    {
        public const string kUDIDFName = "udid" + Defaults.kFileExpKey;
        private static UDIDGen mInstance;
        private static readonly Encoding mEncoding = Encoding.UTF8;

        private FileSystemHelper mFSH = new FileSystemHelper();

        private Guid mGUID;
        private Guid mSessionID;

        protected UDIDGen()
        {
            mGUID = new Guid();
        }

        bool handleUDID()
        {
            bool f = this.existOnDevice();
            if ( !f )
            {
                mGUID = Guid.NewGuid();
                writeUDID();
            }
            return f;
        }

        public void init()
        {
            mSessionID = Guid.NewGuid();
            handleUDID();
        }

        bool doesFileExist(string fileName)
        {
            return mFSH.doesFileExist(fileName);
        }

        bool existOnDevice()
        {
            bool result = doesFileExist(kUDIDFName);
            if ( result )
            {
                BinaryReader br;
                try
                {
                    var iStorageFile = mFSH.getFileStream(kUDIDFName, true);

                    br = new BinaryReader(iStorageFile);

                    var binary = br.ReadBytes(mSessionID.ToByteArray().Length);
                    mGUID = new Guid(binary);
                    br.Dispose();
                    return true;
                }
                catch 
                {
                    //Debug.WriteLine(e.Message + "\n *****Cannot open file or read from it.");
                    mGUID = Guid.NewGuid();
                    return false;
                }
            }

            return false;
        }
        private readonly object _readLock = new object();
        void writeUDID()
        {  
            BinaryWriter bw;

            try
            {
                bw = new BinaryWriter(mFSH.getFileStream(kUDIDFName, false));
                bw.Write(mGUID.ToByteArray());
                bw.Flush();
                bw.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n Cannot create file."); 
            } 
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
                return mEncoding.GetBytes(mSessionID.ToString());
            }
        }

        public Guid SessionIDRaw
        {
            get
            {
                return mSessionID;
            }
        }

        public byte[] UDID
        {
            get
            {
                return mEncoding.GetBytes( mGUID.ToString() );
            }
        }
        public Guid UDIDRaw
        {
            get
            {
                return mGUID ;
            }
        }
    }
}
