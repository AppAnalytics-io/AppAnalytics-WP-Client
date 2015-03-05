﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;

namespace AppAnalytics.UUID
{
    internal class UDIDGen
    {
        private static UDIDGen mInstance;
        private static readonly Encoding mEncoding = Encoding.UTF8;

        private Guid mGUID;
        private Guid mSessionID;

        protected UDIDGen()
        {
            mGUID = new Guid();
            mSessionID = Guid.NewGuid();

            //TODO : check if exist on local machine
            if ( !this.existOnDevice() )
            {
                mGUID = Guid.NewGuid();
                writeUDID();
            }
        }

        public void Init(){}

        private bool existOnDevice()
        {
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();
            if (iStorage.FileExists("udid" + Defaults.kFileExpKey))
            {
                BinaryReader br;
                try
                {
                    lock (_readLock)
                    {
                        br = new BinaryReader(iStorage.OpenFile("udid" + Defaults.kFileExpKey, FileMode.Open));
                        var binary = br.ReadBytes(mSessionID.ToByteArray().Length);
                        mGUID = new Guid(binary);
                        br.Close();
                    }
                    iStorage.Dispose();
                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message + "\n Cannot open file or read from it.");
                    iStorage.Dispose();
                    return false;
                }
            }

            return false;
        }
        private readonly object _readLock = new object();
        private void writeUDID()
        {
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();
            BinaryWriter bw;

            try
            {
                bw = new BinaryWriter(iStorage.OpenFile("udid" + Defaults.kFileExpKey, FileMode.Create));
                bw.Write(mGUID.ToByteArray());
                bw.Flush();
                bw.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }
            iStorage.Dispose();
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
                return mEncoding.GetBytes( mSessionID.ToString() );
            }
        }

        public Guid SessionIDRaw
        {
            get
            {
                return mSessionID;
            }
        }

        public Guid UDIDRaw
        {
            get
            {
                return mGUID;
            }
        }

        public byte[] UDID
        {
            get
            {
                var tst = mGUID.ToString();
                return mEncoding.GetBytes( mGUID.ToString() );
            }
        }
    }
}
