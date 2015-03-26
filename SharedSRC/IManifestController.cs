using System;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Linq;


namespace AppAnalytics
{
    internal interface IManifestController
    {
        void loadData();
        void store();

        bool deserializeManifests(Stream stream, Dictionary<string, byte[]> container);

        bool deserializeSamples(Stream stream,
                            SerializableDictionary<string, List<byte[]>> container);


        bool serializeManifests(Stream stream, Dictionary<string, byte[]> container, bool dispose = true);

        bool serializeSamples(Stream stream,
                            SerializableDictionary<string, List<byte[]>> container, bool dispose = true);

        void buildDataPackage(GestureData aData);

        void buildSessionManifest();

        bool sendManifest();

        bool sendSamples();

        void deleteManifests(List<string> list);

        void deletePackages(Dictionary<string, List<object>> map);

        int SamplesCount
        {
            get;
        }
    }

    internal abstract class AManifestController : IManifestController
    {
        protected readonly object _readLock = new object();
        protected Dictionary<string, byte[]> mManifests = new Dictionary<string, byte[]>();
        protected SerializableDictionary<string, List<byte[]>> mSamples = new SerializableDictionary<string, List<byte[]>>();

#if SILVERLIGHT
        public abstract void store();
        public abstract void loadData();
#else
        public abstract void store();
        public abstract void loadData();
#endif

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
            return Sender.sendSamplesDictAsBinary(mSamples, _readLock);
        } 

        public int SamplesCount
        {
            get
            {
                lock (_readLock)
                {
                    return mSamples.Count;
                }
            }
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

        public bool ContainsCurrentManifest
        {
            get
            {
                bool flag = false;
                lock (_readLock)
                {
                    flag = (mManifests.ContainsKey(Detector.getSessionIDString()))
                        && mManifests[Detector.getSessionIDString()].Length != 0;
                }
                return flag;
            }
        }

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

        public bool serializeManifests(Stream aStream, Dictionary<string, byte[]> aDict, bool aDispose = true)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, byte[]>));

            var bw = aStream;
            serializer.WriteObject(bw, mManifests);
            if (aDispose)
                bw.Dispose();
            return true;
        }

        public bool serializeSamples(Stream aStream, SerializableDictionary<string, List<byte[]>> aDict, bool aDispose = true)
        {
            XmlSerializer xmlSerial = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

            var bw = aStream;
            xmlSerial.Serialize(bw, mSamples);
            if (aDispose)
                bw.Dispose();
            return true;
        }


        public bool deserializeManifests(Stream stream, Dictionary<string, byte[]> container)
        {
            bool flag = true;

            DataContractSerializer serializer1 = new DataContractSerializer(typeof(Dictionary<string, byte[]>));

            try
            {
                container = (Dictionary<string, byte[]>)serializer1.ReadObject(stream);
            }
            catch (Exception)
            {
                flag = false;
            }

            stream.Dispose();

            return flag;
        }

        public bool deserializeSamples(Stream stream,
                            SerializableDictionary<string, List<byte[]>> container)
        {
            bool flag = true;
            XmlSerializer serializer2 =
                new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

            try
            {
                object t = serializer2.Deserialize(stream);
                container = t as SerializableDictionary<string, List<byte[]>>;
                // to do det rid of magic numbers
                if (container.Count > 10000)
                {
                    var toskip = container.Count - 10000;
                    container = (SerializableDictionary<string, List<byte[]>>)
                                container.Skip(toskip).Take(10000).
                                ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
            catch (Exception)
            {
                flag = false;
            }

            stream.Dispose();

            return flag;
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
