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

        async void loadData()
        {
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            SerializableDictionary<string, List<byte[]>> tmpSmpl = new SerializableDictionary<string, List<byte[]>>();
            Dictionary<string, byte[]> tmpMan = new Dictionary<string, byte[]>();

            try
            {
                if (await doesFileExistAsync("manifests", folder))
                {
                    DataContractSerializer serializer1 = new DataContractSerializer(typeof(Dictionary<string, byte[]>));

                    var file = await folder.GetFileAsync("manifests");
                    var stream = await file.OpenStreamForReadAsync();
                    tmpMan = (Dictionary<string, byte[]>)serializer1.ReadObject(stream);
                    if (tmpMan.Count > 100)
                    {
                        var toskip = tmpMan.Count - 100;
                        tmpMan = tmpMan.Skip(toskip).Take(100).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                    stream.Dispose();
                }
                if (await doesFileExistAsync("samples", folder))
                {
                    XmlSerializer serializer2 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

                    var file = await folder.GetFileAsync("samples");
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
                //testing
                #region tst
//                 if (true)
//                 {
//                     mSamples.Clear();
//                     XmlSerializer serializer3 = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));
//                     GestureData tst0 = GestureData.create(GestureID.DoubleTapWith1Finger, new Point(), "1", "1");
//                     GestureData tst1 = GestureData.create(GestureID.DoubleTapWith1Finger, new Point(), "123", "1234567");
//                     GestureData tst2 = GestureData.create(GestureID.DoubleTapWith1Finger, new Point(), "123", "1234567");
// 
//                     buildDataPackage(tst0); buildDataPackage(tst1); buildDataPackage(tst2);
//                     var file = await folder.CreateFileAsync("tst", CreationCollisionOption.ReplaceExisting);
//                     var stream = await file.OpenStreamForWriteAsync();
//                     serializer3.Serialize(stream, mSamples);
//                     stream.Dispose();
// 
//                     stream = await file.OpenStreamForReadAsync();
//                     object t = serializer3.Deserialize(stream);
//                     mSamples = t as SerializableDictionary<string, List<byte[]>>;
//                     stream.Dispose();
//                 }
                #endregion
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n Cannot create file."); 
            }

            lock (_readLock)
            {
                if (tmpMan.Count != 0)
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
            load.Start();
            load.Wait();
        }

        ~ManifestController()
        {
            store();
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

                var fileManifests = await folder.CreateFileAsync("manifests", CreationCollisionOption.ReplaceExisting);
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

                var fileSamples = await folder.CreateFileAsync("samples", CreationCollisionOption.ReplaceExisting);
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
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n Cannot create file.");
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

            const int kMaxAtOnce = 1024 * 100; // 900 * 17
            int bts = 0;
            lock (_readLock)
            {
                foreach (var kval in mSamples)
                {
                    int index = 0;

                    var ms = new MemoryStream();

                    ms.WriteByte((byte)'H');
                    ms.WriteByte((byte)'A');
                    ms.WriteByte(ManifestBuilder.kDataPackageFileVersion);

                    var session = Encoding.UTF8.GetBytes(kval.Key);
                    Debug.Assert(session.Length == 36);

                    ms.Write(Detector.getSessionID(), 0, Detector.getSessionID().Length);

                    wrapper.Add(kval.Key, new MultipartUploader.FileParameter(ms.ToArray()));
                    ms.Dispose();

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


                        index++;
                    }
                    count.Add(index);

                    // just for sending ONE session at once. Ill keep pld code
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

        private readonly object _readLock = new object();  
    }
}
