using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Microsoft.Phone.Net.NetworkInformation;

namespace AppAnalytics
{
    internal static class Sender
    {
#if DEBUG
        public const string kGTBaseURL      = "http://www.appanalytics.io/api/v1/"; // @"http://192.168.1.36:6249/api/v1";
#else
        public const string kGTBaseURL      = "http://wa-api-cent-us-1.cloudapp.net/api/v1/"; //
#endif
        public const string kGTManifestsURL = "manifests?UDID=";
        public const string kGTSamplesURL = "samples?UDID=";

        private const bool kTryToSendMore = true;

        public static readonly object _lockObj = new object();

        private const bool kSimulateSending = true;
//         static List<string> mManifestToDel = new List<string>();
//         static Dictionary<string, List<object>> mPackagesToDel = new Dictionary<string, List<object>>();
//         static Dictionary<string, List<object>> mEventsToDel = new Dictionary<string, List<object>>();


        public static bool tryToSend(AppAnalytics.MultipartUploader.FileParameter aFiles, Dictionary<string, List<object>> aToDel)
        {
            if (NetworkInterface.GetIsNetworkAvailable() == true && aFiles.Count > 0)
            { 
#if DEBUG
                if(kSimulateSending)
                {
                    Sender.success(aFiles.FileType, aToDel);
                    Debug.WriteLine("Sender :: Sending simulated. (only for dbg mode)");
                    return true;
                }
#endif
                bool success = MultipartUploader.MultipartFormDataPut(kGTBaseURL +
                                                                        kGTManifestsURL
                                                                        + Detector.getUDIDString(),
                                                                        "WindowsPhone",
                                                                        aFiles,
                                                                        aToDel);  

                return true;
            }                
            
            return false;
        }

        static private void addRangeToDelList(string name, List<object> aToDel, Dictionary<string, List<object>> aDict)
        {
            if (aDict.ContainsKey(name))
            {
                aDict[name].AddRange(aToDel);
            }
            else
            {
                aDict[name] = aToDel;
            } 
        } 

        public static void success(AAFileType aType, Dictionary<string, List<object> > aToDel)
        {
            Debug.Assert (aToDel != null && aToDel.Count != 0, "assertion failed, Sender.success method") ;
            if (aToDel == null || aToDel.Count == 0) return;
            // to be tested
            if (aType == AAFileType.FTManifests)
            {
                List<string> tmp = aToDel.Keys.ToList();
                ManifestController.Instance.deleteManifests(tmp);
            }
            else if (aType == AAFileType.FTSamples)
            {
                ManifestController.Instance.deletePackages(aToDel);
            }
            else if (aType == AAFileType.FTEvents)
            {
                EventsManager.Instance.deleteEvents(aToDel);
                //throw new NotImplementedException(); 
            } 

            if (kTryToSendMore && (ManifestController.Instance.SamplesCount > 10) )
            {
                ManifestController.Instance.sendSamples();
            }
        }

        public static void fail()
        {
            // should we retry immediately or in regular time
//             lock (_lockObj)
//             {
// 
//             }
        }

        #region sending

        public static bool sendSamplesDictAsBinary(Dictionary<string, List<byte[]>> aSamples, object _readLock)
        {
            Dictionary<string, object> wrapper = new Dictionary<string, object>();
            List<object> toDel = new List<object>();

            const int kMaxAtOnce = 1024 * 100; // 900 * 17
            int bts = 0;
            lock (_readLock)
            {
                foreach (var kval in aSamples)
                {
                    toDel.Add(kval.Value);

                    var ms = new MemoryStream();

                    ms.WriteByte((byte)'H');
                    ms.WriteByte((byte)'A');
                    ms.WriteByte(ManifestBuilder.kDataPackageFileVersion);

                    var session = Encoding.UTF8.GetBytes(kval.Key);
                    Debug.Assert(session.Length == 36);

                    ms.Write(session, 0, session.Length);

                    wrapper.Add(kval.Key, new MultipartUploader.FileParameter(ms.ToArray(), kval.Key, AAFileType.FTSamples));
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
                    }

                }
                return true;
            }
        }

        public static bool sendManifestsAsDict(Dictionary<string, byte[]> aManifests, object _readLock)
        {
            List<object> wrapper = new List<object>();

            lock (_readLock)
            {
                foreach (var kval in aManifests)
                {
                    wrapper.Add(new MultipartUploader.FileParameter(kval.Value, kval.Key, AAFileType.FTManifests));
                }
            }

            foreach (var it in wrapper)
            {
                var mpf = it as MultipartUploader.FileParameter;
                Sender.tryToSend( mpf,
                    new Dictionary<string, List<object>> { {mpf.FileName, null} });
            }

            return true;
        }

        #endregion

    }
}
