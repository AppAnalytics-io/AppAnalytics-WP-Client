using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml.Serialization; 

namespace AppAnalytics
{
	internal class EventsManager
	{
	    #region Members 
        Dictionary<string, List<AAEvent>> mEvents;

        UInt64  mIndex = 0;
        float   mDispatchInterval = Defaults.kDefDispatchInterval;
        bool    mDebugLogEnabled = Defaults.kDbgLogEnabled;
        bool    mExceptionAnalyticsEnabled = Defaults.kExceptionAnalyticsEnabled;
        bool    mTransactionAnaliticsEnabled = Defaults.kTransactionAnalyticsEnabled;
        bool    mScreenAnalitycsEnabled = Defaults.kScreensAnalyticsEnabled;

        Timer   mSerializationTimer = null;
        Timer   mDispatchTimer = null;

        static private readonly object _lockObject = new object();
        #endregion

        private static EventsManager mInstance;
        public static EventsManager Instance
        {
            // singleton
            get { return mInstance ?? (mInstance = new EventsManager()); }
        }

        public string toJSONString(string aKey, out int aCount, out List<object> aToDel)
        {
            aCount = 0;
            aToDel = new List<object>();
            if (mEvents.ContainsKey(aKey) == false)
            {
                return "";
            }
            bool first = true;

            StringBuilder sb = new StringBuilder("{\"Events\":[", 8192);
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
                    sb.Append(string.Format("],\"SessionID\":\"{0}\"}", Detector.getSessionIDStringWithDashes()));
                    return sb.ToString();
                }
            }
            sb.Append(string.Format("],\"SessionID\":\"{0}\"}", Detector.getSessionIDStringWithDashes())); 

            return sb.ToString();
        }

        public void tryToSend()
        {
            int count = 0;
            List<object> toDel;

            foreach (var kval in mEvents)
            {

                var buf = toJSONString(kval.Key, out count, out toDel);
                if (count > 0)
                {
                    byte[] damp = Encoding.UTF8.GetBytes(buf);
                    var param = new MultipartUploader.FileParameter(damp, kval.Key, "application/json", (uint)count, AAFileType.FTEvents);
                    Sender.tryToSend(param, toDel);
                }
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
                        //test it .TODO
                        foreach (var it in kval.Value)
                        {
                            mEvents[kval.Key].RemoveAll( kval.Value.Contains );
                        }
// 
//                         mEvents[kval.Key] = (List<byte[]>)mEvents[kval.Key].
//                             .Except(kval.Value);
                    }
                }
            }
        } 

        public void testSerialization()
        {
//             DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Dictionary<string, AAEvent>));
// 
//             var mdict = new Dictionary<string, List<AAEvent>>();
// 
//             var dd = new Dictionary<string, string>();
//             dd.Add("key", "some_value");
// 
//             mdict.Add("g", AppAnalytics.AAEvent.create(0, 10, "none", dd));
// 
//             var bw = new MemoryStream();
//             
//             js.WriteObject(bw, mdict);
//             bw.Seek(0, SeekOrigin.Begin);
// 
//             var tmp = (Dictionary<string, AppAnalytics.AAEvent>)js.ReadObject(bw);
//             bw.Seek(0, SeekOrigin.Begin);
// 
//             string result = "";
//             using (var streamReader = new StreamReader(bw))
//             {
//                 result = streamReader.ReadToEnd();
//                 Debug.WriteLine( result );
//             } 
// 
//             bw.Dispose();
            
        }
	}
}