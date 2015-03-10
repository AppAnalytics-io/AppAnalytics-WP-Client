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
        private static UDIDGen mInstance;
        private static readonly Encoding mEncoding = Encoding.UTF8;

        private Guid mGUID;
        private Guid mSessionID;

        protected UDIDGen()
        {
            mGUID = new Guid();
            mSessionID = Guid.NewGuid(); 
        }

        async Task<bool> handleUDID()
        {
            bool f = await this.existOnDevice();
            if ( !f )
            {
                mGUID = Guid.NewGuid();
                await writeUDID();
            }
            return f;
        }

        public async void init()
        {
            await handleUDID();
        }

        async Task<bool> doesFileExistAsync(string fileName, StorageFolder folder) 
        {
	        try 
            {
                await folder.GetFileAsync(fileName);
		        return true;
	        } catch {
		        return false;
	        }
        }

        async Task<bool> existOnDevice()
        {
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;

            bool result = await doesFileExistAsync("udid" + Defaults.kFileExpKey, local);  
            if ( result )
            {
                BinaryReader br;
                try
                {
                    var iStorageFile = await local.GetFileAsync("udid" + Defaults.kFileExpKey);

                    br = new BinaryReader(await iStorageFile.OpenStreamForReadAsync());
                    
                    var binary = br.ReadBytes(mSessionID.ToByteArray().Length);
                    mGUID = new Guid(binary); 
                    br.Dispose();
                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message + "\n *****Cannot open file or read from it.");
                    mGUID = Guid.NewGuid();
                    return false;
                } 
            }

            return false;
        }
        private readonly object _readLock = new object();
        async Task<bool> writeUDID()
        {
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile iStorage = await local.CreateFileAsync("udid" + Defaults.kFileExpKey, CreationCollisionOption.ReplaceExisting);
            BinaryWriter bw;

            try
            {
                bw = new BinaryWriter(await iStorage.OpenStreamForWriteAsync());
                bw.Write(mGUID.ToByteArray());
                bw.Flush();
                bw.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n Cannot create file.");
                return false;
            }
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
