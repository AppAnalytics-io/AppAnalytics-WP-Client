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
    class ManifestController : AManifestController
    {
        private static ManifestController mInstance;
        public static ManifestController Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new ManifestController()); }
        }

        protected ManifestController()
        {
            loadData();
        }

        ~ManifestController()
        {
            store();
        } 

        override public void loadData()
        {
            var tmpSmpl = new SerializableDictionary<string, List<byte[]>>();
            var tmpMan = new Dictionary<string, byte[]>();
            try
            {
                var fh = new FileSystemHelper();
                 
                Stream stream = fh.getFileStream("manifests" + Defaults.kFileExpKey, true);
                if (null != stream)
                {
                    deserializeManifests(stream, tmpMan);
                }

                stream = fh.getFileStream("samples" + Defaults.kFileExpKey, true);
                if (stream != null)
                {
                    deserializeSamples(stream, tmpSmpl);
                } 
            }
            catch (Exception)
            {  }

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
        }

        override public void store()
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
// 
//         public void deletePackages(Dictionary<string, List<object>> map)
//         {
//             lock (_readLock)
//             {
//                 var tmp = new SerializableDictionary<string, List<byte[]>>();
//                 List<byte[]> tmp2 = new List<byte[]>(); 
//                 foreach (var kval in map)
//                 {
//                     if (mSamples.ContainsKey(kval.Key) && (mSamples[kval.Key].Count >= kval.Value.Count))
//                     {
//                         mSamples[kval.Key] = mSamples[kval.Key].Except(kval.Value).Cast<byte[]>().ToList(); 
//                     }
//                 }  
//                 var copyS = new SerializableDictionary<string, List<byte[]>>(mSamples);
//                 foreach (var kv in mSamples)
//                 {
//                     if (kv.Value.Count == 0) 
//                         copyS.Remove(kv.Key);
//                 }
//                 mSamples = copyS;
//             }
//         }

    }
}
