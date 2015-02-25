using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;

using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace AppAnalytics
{
    public enum GestureID
    {
        SingleTapWith1Finger = 1,
        DoubleTapWith1Finger = 2,
        TripleTapWith1Finger = 3,
        SingleTapWith2Finger = 4,
        DoubleTapWith2Finger = 5,
        TripleTapWith2Finger = 6,
        SingleTapWith3Finger = 7,
        DoubleTapWith3Finger = 8,
        TripleTapWith3Finger = 9,
        SingleTapWith4Finger = 10,
        DoubleTapWith4Finger = 11,
        TripleTapWith4Finger = 12,
        HoldWith1Finger = 13,
        HoldWith2Finger = 14,
        HoldWith3Finger = 15,
        HoldWith4Finger = 16,
        PinchWith2Finger = 17,
        ZoomWith2Finger = 18,
        RotateWith2Finger = 19,
        SwipeRightWith1Finger = 20,
        SwipeLeftWith1Finger = 21,
        SwipeDownWith1Finger = 22,
        SwipeUpWith1Finger = 23,
        FlickRightWith1Finger = 24,
        FlickLeftWith1Finger = 25,
        FlickDownWith1Finger = 26,
        FlickUpWith1Finger = 27,
        SwipeRightWith2Finger = 28,
        SwipeLeftWith2Finger = 29,
        SwipeDownWith2Finger = 30,
        SwipeUpWith2Finger = 31,
        FlickRightWith2Finger = 32,
        FlickLeftWith2Finger = 33,
        FlickDownWith2Finger = 34,
        FlickUpWith2Finger = 35,
        SwipeRightWith3Finger = 36,
        SwipeLeftWith3Finger = 37,
        SwipeDownWith3Finger = 38,
        SwipeUpWith3Finger = 39,
        FlickRightWith3Finger = 40,
        FlickLeftWith3Finger = 41,
        FlickDownWith3Finger = 42,
        FlickUpWith3Finger = 43,
        SwipeRightWith4Finger = 44,
        SwipeLeftWith4Finger = 45,
        SwipeDownWith4Finger = 46,
        SwipeUpWith4Finger = 47,
        FlickLeftWith4Finger = 48,
        FlickRightWith4Finger = 49,
        FlickDownWith4Finger = 50,
        FlickUpWith4Finger = 51,
        Shake = 52,
        Navigation = 53
    }

    class ManifestController
    {
        private static ManifestController mInstance;
        public static ManifestController Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new ManifestController()); }
        }

        protected ManifestController()
        {
            mContent = new MemoryStream();
            mPackage = new MemoryStream();
            //
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();

            try
            {
                lock (_readLock)
                {
                    if (iStorage.FileExists("manifests"))
                    {
                        DataContractSerializer serializer1 = new DataContractSerializer(typeof(Dictionary<string, byte[]>));

                        var bw = iStorage.OpenFile("manifests", FileMode.Open);
                        mManifests = (Dictionary<string, byte[]>)serializer1.ReadObject(bw);
                        if (mManifests.Count > 100)
                        {
                            var toskip = mManifests.Count - 100;
                            mManifests = mManifests.Skip(toskip).Take(100).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        }
                        bw.Close();
                    }
                    if (iStorage.FileExists("samples"))
                    {
                        XmlSerializer serializer2 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                        var bw = iStorage.OpenFile("samples", FileMode.Open);
                        object t = serializer2.Deserialize(bw);
                        mSamples = t as SerializableDictionary<string, List<byte[]>>;
                        if (mSamples.Count > 10000)
                        {
                            var toskip = mSamples.Count - 10000;
                            mSamples = (SerializableDictionary<string, List<byte[]>>)
                                mSamples.Skip(toskip).Take(10000).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        }
                        bw.Close();
                    }

                    //testing
                    #region tst
                    if (false)
                    {
                        mSamples.Clear();
                        XmlSerializer serializer3 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));
                        GestureData tst0 = GestureData.create(GestureID.DoubleTapWith1Finger, new System.Windows.Point(), "1", "1");
                        GestureData tst1 = GestureData.create(GestureID.DoubleTapWith1Finger, new System.Windows.Point(), "123", "1234567");
                        GestureData tst2 = GestureData.create(GestureID.DoubleTapWith1Finger, new System.Windows.Point(), "123", "1234567");

                        buildDataPackage(tst0); buildDataPackage(tst1); buildDataPackage(tst2);

                        var bw = iStorage.OpenFile("tst", FileMode.Create);
                        serializer3.Serialize(bw, mSamples);
                        bw.Close();

                        var stream = iStorage.OpenFile("tst", FileMode.Open);
                        object t = serializer3.Deserialize(stream);
                        mSamples = t as SerializableDictionary<string, List<byte[]>>;
                        stream.Close();
                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            iStorage.Dispose();
        }

        ~ManifestController()
        {
            store();
        }

        public void store()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, byte[]>));
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();

            lock (_readLock)
            {
                try
                {
                    if (mManifests.Count > 0)
                    {
                        var bw = iStorage.OpenFile("manifests", FileMode.Create);
                        serializer.WriteObject(bw, mManifests);
                        bw.Close();
                    }
                    else
                    {
                        iStorage.DeleteFile("manifests");
                    }

                    if (mSamples.Count > 0)
                    {
                        XmlSerializer serializer3 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                        var bw = iStorage.OpenFile("samples", FileMode.Create);
                        serializer3.Serialize(bw, mSamples);
                        bw.Close();
                    }
                    else
                    {
                        iStorage.DeleteFile("samples");
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message + "\n Cannot create file.");
                    return;
                }
            }

            iStorage.Dispose();
        }

        private byte kDataPackageFileVersion = 1;
        private byte kSessionManifestFileVersion = 1;

        private MemoryStream mPackage;
        private MemoryStream mContent;

        private Dictionary<string, byte[]> mManifests = new Dictionary<string,byte[]>();
        private SerializableDictionary<string, List<byte[]>> mSamples = new SerializableDictionary<string, List<byte[]>>();

        
        private void writeArray(MemoryStream aMS, byte[] aBlock)
        {
            aMS.Write(aBlock, 0, aBlock.Length);
        }

        public void buildDataPackage(GestureData aData)
        {
            mPackage.WriteByte((byte)'H');
            mPackage.WriteByte((byte)'A');
            mPackage.WriteByte(kDataPackageFileVersion);

            mPackage.Write(Detector.getSessionID(), 0, Detector.getSessionID().Length);

            mPackage.WriteByte((byte)'<');

            writeArray(mPackage, aData.ActionOrder);
            mPackage.WriteByte(aData.ActionID);
            writeArray(mPackage, aData.ActionTime);
            writeArray(mPackage, aData.PosX);
            writeArray(mPackage, aData.PosY);
            writeArray(mPackage, aData.Param1);
            writeArray(mPackage, BitConverter.GetBytes(aData.ViewIDLenght));
            writeArray(mPackage, aData.ViewID);
            writeArray(mPackage, BitConverter.GetBytes(aData.ElementIDLenght));
            writeArray(mPackage, aData.ElementID);

            mPackage.WriteByte((byte)'>');
            lock (_readLock)
            {
                if (mSamples.ContainsKey(Detector.getSessionIDString()))
                {
                    mSamples[Detector.getSessionIDString()].Add( mPackage.ToArray() );
                }
                else
                {
                    mSamples[Detector.getSessionIDString()] = new List<byte[]>();
                    mSamples[Detector.getSessionIDString()].Add(mPackage.ToArray());
                }
            }
            mPackage.Close();
            mPackage = new MemoryStream();
        }

        public void buildSessionManifest()
        {
            mContent.WriteByte((byte)'<');
            mContent.WriteByte(kSessionManifestFileVersion);
            writeArray(mContent, Detector.getSessionID());

            writeArray(mContent, Detector.getSessionStartDate());

            writeArray(mContent, Detector.getSessionEndDate());

            writeArray(mContent, Detector.getUDID().Take(32).ToArray() ); // 90 != 85 => cropping

            writeArray(mContent, Detector.getResolutionX());
            writeArray(mContent, Detector.getResolutionY());

            mContent.WriteByte(  Detector.ApiVersion);

            writeArray(mContent, Detector.ApiKey);
            writeArray(mContent, Detector.AppVersion);
            writeArray(mContent, Detector.OSVersion);
            writeArray(mContent, Detector.SystemLocale);

            mContent.WriteByte((byte)'>');
            lock (_readLock)
            {
                mManifests[Detector.getSessionIDString()] = mContent.ToArray();
            }
            var t = mContent.ToArray();
            mContent.Close();
            mContent = new MemoryStream();
        }

        public bool sendManifest()
        {
            Dictionary<string, object> wrapper = new Dictionary<string,object>(); 

            lock (_readLock)
            {
                foreach ( var kval in mManifests)
                {
                    wrapper.Add(kval.Key, new MultipartUploader.FileParameter( kval.Value ));
                }
            }

            bool flag = Sender.tryToSend(wrapper, true);

            return flag;
        }
        public bool sendSamples()
        {
            Dictionary<string, object> wrapper = new Dictionary<string, object>();
            List<int> count = new List<int>();

            const int kMaxAtOnce = 1024*100; // 900 * 17
            int bts = 0;
            lock (_readLock)
            {
                foreach (var kval in mSamples)
                {
                    int index  = 0;
                    foreach (var gst in kval.Value)
                    {
                        bts += gst.Length;
                        if (bts > kMaxAtOnce)
                        {
                            break;
                        }
                        if (wrapper.ContainsKey(kval.Key))
                        {
                            var arr = wrapper[kval.Key] as MultipartUploader.FileParameter;
                            if (arr != null)
                            {
                                arr.File = arr.File.Concat(gst).ToArray();
                            }
                            else Debug.WriteLine("logical error: MC sendSamples()");
                        }
                        else
                        {
                            wrapper.Add(kval.Key, new MultipartUploader.FileParameter(gst));
                        }
                        index ++;
                    }
                    count.Add(index);
                }
            }

            bool flag = Sender.tryToSend(wrapper, false, count);

            return flag;
        }

        public void deleteManifests(List<string> list)
        {
            lock (_readLock)
            {
                foreach (var item in list)
                {
                    mManifests.Remove(item);
                }
            }
        }

        public int SamplesCount
        {
            get
            {
                lock ( _readLock)
                {
                    return mSamples.Count;
                }
            }
        }

        public void deletePackages(Dictionary<string, int> map)
        {
            lock (_readLock)
            {
                foreach (var kval in map)
                {
                    if (mSamples.ContainsKey(kval.Key) && (mSamples[kval.Key].Count >= kval.Value))
                    {
                        mSamples[kval.Key].RemoveRange(0, kval.Value);
                    }
                }
                //mSamples.
                var copyS = new SerializableDictionary<string, List<byte[]>> (mSamples);
                foreach (var kv in mSamples)
                {
                    if (kv.Value.Count == 0) copyS.Remove(kv.Key);
                }
                mSamples = copyS;
            }
        } 

        private readonly object _readLock = new object();  
    }
}
