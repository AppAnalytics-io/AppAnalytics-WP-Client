using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using System.Diagnostics;

using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Windows.Foundation;
using System.Threading;
using System.Net.NetworkInformation;

namespace AppAnalytics
{
    class ManifestController
    {
        private static ManifestController mInstance;
        public static ManifestController Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new ManifestController()); }
        }

        async Task<bool> doesFileExistAsync(string fileName, StorageFolder folder)
        {
            try
            {
                await folder.GetFileAsync(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        AutoResetEvent mSyncEvent = new AutoResetEvent(false);

        // temporary version. will be rewritten after tests.
        const int kMaxTasks = 4;
        Task[] mManifestTaskPool = new Task[5];
        Task[] mSamplesTaskPool = new Task[5];
        //.

        async void loadData()
        {
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            SerializableDictionary<string, List<byte[]>> tmpSmpl = new SerializableDictionary<string, List<byte[]>>();
            Dictionary<string, byte[]> tmpMan = new Dictionary<string, byte[]>();

            try
            {
                if (await doesFileExistAsync("manifests" + Defaults.kFileExpKey, folder))
                {
                    DataContractSerializer serializer1 = new DataContractSerializer(typeof(Dictionary<string, byte[]>));

                    var file = await folder.GetFileAsync("manifests" + Defaults.kFileExpKey);
                    var stream = await file.OpenStreamForReadAsync();
                    tmpMan = (Dictionary<string, byte[]>)serializer1.ReadObject(stream);
                    if (tmpMan.Count > 100)
                    {
                        var toskip = tmpMan.Count - 100;
                        tmpMan = tmpMan.Skip(toskip).Take(100).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                    stream.Dispose();
                }
                if (await doesFileExistAsync("samples" + Defaults.kFileExpKey, folder))
                {
                    XmlSerializer serializer2 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                    var file = await folder.GetFileAsync("samples" + Defaults.kFileExpKey);
                    var stream = await file.OpenStreamForReadAsync();
                    object t = serializer2.Deserialize(stream);

                    tmpSmpl = t as SerializableDictionary<string, List<byte[]>>;
                    if (tmpSmpl.Count > 10000)
                    {
                        var toskip = tmpSmpl.Count - 10000;
                        tmpSmpl = (SerializableDictionary<string, List<byte[]>>)
                            tmpSmpl.Skip(toskip).Take(10000).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                    stream.Dispose();
                }
            }
            catch 
            {
                //Debug.WriteLine(e.Message + "\n Cannot create file.");
            }

            lock (_readLock)
            {
                if (tmpMan.Count != 0 && false)
                {
                    mManifests = tmpMan;
                }
               if (tmpSmpl.Count != 0)
                {
                    mSamples = tmpSmpl;
                }
            }

            mSyncEvent.Set();
        }

        protected ManifestController()
        {
            mSyncEvent.Reset();
            mContent = new MemoryStream();
            mPackage = new MemoryStream();
            //
            Task load = new Task(loadData);
            load.Start(); // maybe we can @fire and forget@ about this. will think about it.
            load.Wait();
        }

        ~ManifestController()
        {
            var tmp = new Task(store);
            tmp.Start();
            tmp.Wait(); // storing samples that are not sent yet.
        }

        public async void store()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, byte[]>));
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;

            try
            {
                Dictionary<string, byte[]> tmpMan;
                SerializableDictionary<string, List<byte[]>> tmpSmpl;
                lock (_readLock)
                {
                    tmpMan = new Dictionary<string, byte[]>(mManifests);
                    tmpSmpl = new SerializableDictionary<string, List<byte[]>>(mSamples);
                }

                var fileManifests = await folder.CreateFileAsync("manifests" + Defaults.kFileExpKey, CreationCollisionOption.ReplaceExisting);
                if (mManifests.Count > 0)
                {
                    var bw = await fileManifests.OpenStreamForWriteAsync();
                    serializer.WriteObject(bw, mManifests);
                    bw.Dispose();
                }
                else
                {
                    await fileManifests.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                var fileSamples = await folder.CreateFileAsync("samples" + Defaults.kFileExpKey, CreationCollisionOption.ReplaceExisting);
                if (mSamples.Count > 0)
                {
                    XmlSerializer xmlSerial = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                    var bw = await fileSamples.OpenStreamForWriteAsync();
                    xmlSerial.Serialize(bw, mSamples);
                    bw.Dispose();
                }
                else
                {
                    await fileSamples.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch 
            {
                //Debug.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }
        }

        private MemoryStream mPackage;
        private MemoryStream mContent;

        private Dictionary<string, byte[]> mManifests = new Dictionary<string,byte[]>();
        private SerializableDictionary<string, List<byte[]>> mSamples = new SerializableDictionary<string, List<byte[]>>();

        public void buildDataPackage(GestureData aData)
        {
            ManifestBuilder.buildDataPackage(aData, mSamples, _readLock);
        }

        public void buildSessionManifest()
        {
            mSyncEvent.WaitOne();
            ManifestBuilder.buildSessionManifest(mManifests, _readLock);
        }

        public bool sendManifest()
        {
            return Sender.sendManifestsAsDict(mManifests, _readLock);
        }

        public bool sendSamples()
        {
            return Sender.sendSamplesDictAsBinary(mSamples, _readLock);
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

        private readonly object _readLock = new object();
    }
}
