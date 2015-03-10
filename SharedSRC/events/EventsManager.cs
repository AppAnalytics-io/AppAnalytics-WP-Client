using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

#if SILVERLIGHT
using System.IO.IsolatedStorage;
using System.Windows.Threading;
#else
using Windows.Storage;
using Windows.UI.Xaml;
#endif

namespace AppAnalytics
{
	internal class EventsManager
	{
	    #region Members
        Dictionary<string, List<AAEvent>> mEvents = new Dictionary<string,List<AAEvent>>();

        UInt32  mIndex = 0;
        float   mDispatchInterval = Defaults.kDefDispatchInterval;
        bool    mDebugLogEnabled = Defaults.kDbgLogEnabled;

        bool    mExceptionAnalyticsEnabled = Defaults.kExceptionAnalyticsEnabled;

        bool mTransactionAnaliticsEnabled = Defaults.kTransactionAnalyticsEnabled;

        public bool TransactionAnaliticsEnabled
        {
            get { return mTransactionAnaliticsEnabled; }
            set { mTransactionAnaliticsEnabled = value; }
        }
        bool    mScreenAnalitycsEnabled = Defaults.kScreensAnalyticsEnabled;


        Timer mSerializationTimer = null;
        Timer mDispatchTimer = null;

        public bool DebugLogEnabled
        {
            get { return mDebugLogEnabled; }
            set { mDebugLogEnabled = value; }
        }
        public bool ExceptionAnalyticsEnabled
        {
            get { return mExceptionAnalyticsEnabled; }
            set { mExceptionAnalyticsEnabled = value; }
        }
        public float DispatchInterval
        {
            get { return mDispatchInterval; }
            set
            {
                var tmp =   (value > Defaults.kMaxDispatchInterval) ? Defaults.kMaxDispatchInterval : value;
                tmp =       (value < Defaults.kMinDispatchInterval) ? Defaults.kMinDispatchInterval : value;

                mDispatchInterval = tmp;
            }
        }
        public bool ScreenAnalitycsEnabled
        {
            get { return mScreenAnalitycsEnabled; }
            set { mScreenAnalitycsEnabled = value; }
        }

        static private readonly object _lockObject = new object();
        #endregion

        private static EventsManager mInstance;
        public void init() { }

        ~EventsManager()
        {
            serialize(null);
        }

        public void store()
        {
            serialize(null);
        }

        EventsManager()
        {
            var t2 = new Task(deserialize);
            t2.Start(); t2.Wait();

            mEvents.Add(Detector.getSessionIDStringWithDashes(), new List<AAEvent>());

            mDispatchTimer = new Timer(tryToSendCallback, null,
#if SILVERLIGHT
                         (uint)(mDispatchInterval * 1000),
                         (uint)(mDispatchInterval * 1000)
#else
                         (int)(mDispatchInterval * 1000),
                         (int)(mDispatchInterval * 1000)
#endif
            );

            mSerializationTimer = new Timer(serialize, null, (15 * 1000), (15 * 1000));
        }

        #region public_methods
        public static EventsManager Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new EventsManager()); }
        }

        public void pushEvent(string aDescription, Dictionary<string, string> aParams)
        {
            if (aDescription.Length > Defaults.kMaxLogEventStrLen)
            {
                aDescription = aDescription.Substring(0, (int)Defaults.kMaxLogEventStrLen);
            }
            AAEvent newOne = AAEvent.create( 0, 0, aDescription, aParams);
            Detector.logEventDbg(newOne);

            lock (_lockObject)
            {
                pushNewOrUpdateExisted(mEvents[Detector.getSessionIDStringWithDashes()], newOne);
            }
        }

        private void pushNewOrUpdateExisted(List<AAEvent> aContainer, AAEvent aNewOne)
        {
            if (aContainer.Contains(aNewOne))
            {
                aNewOne = aContainer.Find(x => x == aNewOne);
                aNewOne.addIndex(mIndex);
            }
            else
            {
                mEvents[Detector.getSessionIDStringWithDashes()].Add(aNewOne);
            }

            aNewOne.addTimestamp(Detector.getCurrentDouble());
            aNewOne.addIndex(mIndex++);
        }

        public void pushEvent(string aDescription)
        {
            pushEvent(aDescription, null);
        }
        #endregion

        private string toJSONString(string aKey, out int aCount, out List<object> aToDel)
        {
            aCount = 0;
            aToDel = new List<object>();

            StringBuilder sb = new StringBuilder("{\"Events\":[", 8192);

            lock (_lockObject)
            {
                if (mEvents.ContainsKey(aKey) == false)
                {
                    return "";
                }
                bool first = true;
                foreach (var val in mEvents[aKey])
                {
                    aCount++;
                    aToDel.Add(val);

                    if (!first)
                    {
                        sb.Append(',').Append(val.getJsonString());
                    }
                    else
                    {
                        sb.Append(val.getJsonString());
                        first = false;
                    }

                    if (sb.Length > Defaults.kMaxPacketSize)
                    {
                        sb.Append(string.Format("],\"SessionID\":\"{0}\"}}", Detector.getSessionIDStringWithDashes()));
                        return sb.ToString();
                    }
                }
            }

            var session = Detector.getSessionIDStringWithDashes();
            sb.Append(string.Format("],\"SessionID\":\"{0}\"}}", session));
            //Debug.WriteLine("JSON :: :: :: \n" + sb.ToString());

            return sb.ToString();
        }

        public void tryToSendCallback(object obj)
        {
            int count = 0;
            List<object> toDel;
            Dictionary<string, List<object>> wrapper = new Dictionary<string, List<object>>();

            Dictionary<string, List<AAEvent>> copy;
            lock (_lockObject)
            {
                copy = new Dictionary<string, List<AAEvent>>(mEvents);
            }

            foreach (var kval in copy)
            {
                var buf = toJSONString(kval.Key, out count, out toDel);

                if (count > 0)
                {
                    byte[] damp = Encoding.UTF8.GetBytes(buf);
                    var param = new MultipartUploader.FileParameter(damp, kval.Key, "application/json", (uint)count, AAFileType.FTEvents);
                    wrapper.Add(kval.Key, toDel);
                    Sender.tryToSend(param, wrapper);
                }
                wrapper.Clear();
            }
        }

        public void deleteEvents(Dictionary<string, List<object>> map)
        {
            lock (_lockObject)
            {
                foreach (var kval in map)
                {
                    if (mEvents.ContainsKey(kval.Key) && (mEvents[kval.Key].Count >= kval.Value.Count))
                    {
                        foreach (var it in kval.Value)
                        {
                            mEvents[kval.Key].RemoveAll( kval.Value.Contains );
                        }
                        if (mEvents[kval.Key].Count == 0 && kval.Key != Detector.getSessionIDStringWithDashes())
                        {
                            mEvents.Remove(kval.Key);
                        }
                    }
                }
            }
        }

#pragma warning disable 1998 // disabling silverlight no-await warning
        private async Task<Stream> getFileStream(bool aRead)
        {
            Stream stream = null;
#if SILVERLIGHT
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();
            stream = iStorage.OpenFile("aa_events" + Defaults.kFileExpKey, aRead ? FileMode.Open : FileMode.Create);
#else
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = aRead ?
                await folder.GetFileAsync("aa_events" + Defaults.kFileExpKey) :
                await folder.CreateFileAsync("aa_events" + Defaults.kFileExpKey,
                                            CreationCollisionOption.ReplaceExisting);

            stream = await (aRead ? file.OpenStreamForReadAsync() : file.OpenStreamForWriteAsync());
#endif
            return stream;
        }

        private async void serialize(object timer)
        {
            var js = new DataContractJsonSerializer(typeof(Dictionary<string, List<AAEvent>>));
            try
            {
                using (var stream = await getFileStream(false))
                {
                    var tmp = new Dictionary<string, List<AAEvent>>();
                    // unable to use linq selector via silverlight
                    lock (_lockObject)
                    {
                        foreach (var kval in mEvents)
                        {
                            if (kval.Value.Count > 0)
                            {
                                tmp.Add(kval.Key, kval.Value);
                            }
                        }
                    }
                    if (tmp.Count > 0)
                        js.WriteObject(stream, tmp);
                }
            }
            catch {  }
        }

        private async void deserialize()
        {
            var js = new DataContractJsonSerializer(typeof(Dictionary<string, List<AAEvent>>));
            try
            {
                using (var stream = await getFileStream(true))
                {
                    lock (_lockObject)
                        mEvents = (Dictionary<string, List<AAEvent>>)js.ReadObject(stream);
                    if (mEvents == null)
                        mEvents = new Dictionary<string, List<AAEvent>>();
                }
            }
            catch { }   // occurs when aa_events does not exist, it is ok.
                        // it is way to check if file exist with winrt btw :)
        }

#if DEBUG
        // temporary tests, just for debug
        public void testSerializationUsingMemoryStream()
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Dictionary<string, List<AAEvent>>));

            var mdict = new Dictionary<string, List<AAEvent>>();

            var dd = new Dictionary<string, string>();
            dd.Add("key", "some_value");

            var list = new List<AAEvent>();
            list.Add( AppAnalytics.AAEvent.create(0, 10, "none", dd) );
            mdict.Add("g", list);

            var bw = new MemoryStream();

            js.WriteObject(bw, mdict);
            bw.Seek(0, SeekOrigin.Begin);

            var tmp = (Dictionary<string, List<AAEvent>>)js.ReadObject(bw);
            bw.Seek(0, SeekOrigin.Begin);

            string result = "";
            using (var streamReader = new StreamReader(bw))
            {
                result = streamReader.ReadToEnd();
                //Debug.WriteLine( result );
            }
            Debug.Assert(mdict.Values == tmp.Values);
            bw.Dispose();
        }

        public void testSerialization()
        {
            var session = Detector.getSessionIDStringWithDashes();
            if ( mEvents[session].Count > 0)
            {
                mEvents[session].Clear();
            }

            pushEvent("test");
            var t1 = new Task(() => serialize(null) );
            var t2 = new Task(deserialize);
            t1.Start(); t1.Wait();
            t2.Start(); t2.Wait();
            Debug.Assert(mEvents[session].Contains( AAEvent.create(0,0,"test", null) ) );
        }

        public void testSending()
        {
            pushEvent("send_1");
            pushEvent("send_2");
            pushEvent("send_3");
            tryToSendCallback(null);
        }
#endif
	}
}