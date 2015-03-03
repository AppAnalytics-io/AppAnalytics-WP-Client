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
    class ManifestController
    {
        private readonly object _readLock = new object();  
        private static ManifestController mInstance;
        public static ManifestController Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new ManifestController()); }
        }

        protected ManifestController()
        {
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
//                         mSamples.Clear();
//                         XmlSerializer serializer3 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));
//                         GestureData tst0 = GestureData.create(GestureID.DoubleTapWith1Finger, new System.Windows.Point(), "1", "1");
//                         GestureData tst1 = GestureData.create(GestureID.DoubleTapWith1Finger, new System.Windows.Point(), "123", "1234567");
//                         GestureData tst2 = GestureData.create(GestureID.DoubleTapWith1Finger, new System.Windows.Point(), "123", "1234567");
// 
//                         buildDataPackage(tst0); buildDataPackage(tst1); buildDataPackage(tst2);
// 
//                         var bw = iStorage.OpenFile("tst", FileMode.Create);
//                         serializer3.Serialize(bw, mSamples);
//                         bw.Close();
// 
//                         var stream = iStorage.OpenFile("tst", FileMode.Open);
//                         object t = serializer3.Deserialize(stream);
//                         mSamples = t as SerializableDictionary<string, List<byte[]>>;
//                         stream.Close();
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

        private Dictionary<string, byte[]> mManifests = new Dictionary<string,byte[]>();
        private SerializableDictionary<string, List<byte[]>> mSamples = new SerializableDictionary<string, List<byte[]>>();

        public void buildDataPackage(GestureData aData)
        {
            ManifestBuilder.buildDataPackage(aData, mSamples, _readLock);
        }

        public void buildSessionManifest()
        {
            ManifestBuilder.buildSessionManifest(mManifests, _readLock);
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

                    var ms = new MemoryStream();

                    ms.WriteByte((byte)'H');
                    ms.WriteByte((byte)'A');
                    ms.WriteByte(ManifestBuilder.kDataPackageFileVersion);

                    var session = Encoding.UTF8.GetBytes(kval.Key);
                    Debug.Assert(session.Length == 36);

                    ms.Write(session, 0, session.Length);
                    
                    wrapper.Add( kval.Key, new MultipartUploader.FileParameter(ms.ToArray()) );
                    ms.Close();

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


                        index ++;
                    }
                    count.Add(index);

                    // just for sending ONE session at once. Ill keep old code
                    // in case if logic changes
                    break;
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

    }
}
