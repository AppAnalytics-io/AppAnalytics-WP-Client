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
            try
            {
                IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();
                lock (_readLock)
                {
                    if (iStorage.FileExists("manifests" + Defaults.kFileExpKey))
                    {
                        DataContractSerializer serializer1 = new DataContractSerializer(typeof(Dictionary<string, byte[]>));

                        var bw = iStorage.OpenFile("manifests" + Defaults.kFileExpKey, FileMode.Open);
                        mManifests = (Dictionary<string, byte[]>)serializer1.ReadObject(bw);
                        if (mManifests.Count > 100)
                        {
                            var toskip = mManifests.Count - 100;
                            mManifests = mManifests.Skip(toskip).Take(100).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        }
                        bw.Close();
                    }
                    if (iStorage.FileExists("samples" + Defaults.kFileExpKey))
                    {
                        XmlSerializer serializer2 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                        var bw = iStorage.OpenFile("samples" + Defaults.kFileExpKey, FileMode.Open);
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
                } 
                iStorage.Dispose();
            }
            catch 
            {  }
        }

        ~ManifestController()
        {
            store();
        }

        public void store()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, byte[]>));
            IsolatedStorageFile iStorage = null;
            try
            {
                iStorage = IsolatedStorageFile.GetUserStoreForApplication();
            }
            catch { return; }

            lock (_readLock)
            {
                try
                {
                    if (mManifests.Count > 0)
                    {
                        var bw = iStorage.OpenFile("manifests" + Defaults.kFileExpKey, FileMode.Create);
                        serializer.WriteObject(bw, mManifests);
                        bw.Close();
                    }
                    else
                    {
                        iStorage.DeleteFile("manifests" + Defaults.kFileExpKey);
                    }

                    if (mSamples.Count > 0)
                    {
                        XmlSerializer serializer3 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                        var bw = iStorage.OpenFile("samples" + Defaults.kFileExpKey, FileMode.Create);
                        serializer3.Serialize(bw, mSamples);
                        bw.Close();
                    }
                    else
                    {
                        iStorage.DeleteFile("samples" + Defaults.kFileExpKey);
                    }

                }
                catch 
                {
                    return;
                }
            }

            iStorage.Dispose();
        }

        private Dictionary<string, byte[]> mManifests = new Dictionary<string,byte[]>();
        private SerializableDictionary<string, List<byte[]>> mSamples = new SerializableDictionary<string, List<byte[]>>();

        public void buildDataPackage(GestureData aData)
        {
            int count = 0;
            lock (_readLock)
            {
                count = mSamples.Count;
            }
            if (count > 10000) return;
            ManifestBuilder.buildDataPackage(aData, mSamples, _readLock);
        }

        public void buildSessionManifest()
        {
            ManifestBuilder.buildSessionManifest(mManifests, _readLock);
        }

        public bool sendManifest()
        {
           return Sender.sendManifestsAsDict(mManifests, _readLock);
        }
        public bool sendSamples()
        {
            return Sender.sendSamplesDictAsBinary( mSamples, _readLock);
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

        public void deletePackages(Dictionary<string, List<object>> map)
        {
            lock (_readLock)
            {
                var tmp = new SerializableDictionary<string, List<byte[]>>();
                List<byte[]> tmp2 = new List<byte[]>(); 
                foreach (var kval in map)
                {
                    if (mSamples.ContainsKey(kval.Key) && (mSamples[kval.Key].Count >= kval.Value.Count))
                    {
                        mSamples[kval.Key] = mSamples[kval.Key].Except(kval.Value).Cast<byte[]>().ToList(); 
                    }
                }  
                var copyS = new SerializableDictionary<string, List<byte[]>>(mSamples);
                foreach (var kv in mSamples)
                {
                    if (kv.Value.Count == 0) 
                        copyS.Remove(kv.Key);
                }
                mSamples = copyS;
            }
        }

    }
}
