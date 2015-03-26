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
// #if DEBUG
//         public const string kGTBaseURL      = "http://www.appanalytics.io/api/v1/"; // @"http://192.168.1.36:6249/api/v1";
// #else
        public const string kGTBaseURL      = "http://wa-api-cent-us-1.cloudapp.net/api/v1/"; //
/*#endif*/
        public const string kGTManifestsURL = "manifests?UDID=";
        public const string kGTSamplesURL = "samples?UDID=";
        public const string kGTEventsURL = "events?UDID=";

        private const bool kTryToSendMore = true; 

//        public const bool kSimulateSending = false; 

        static string typeToURL(AAFileType ft)
        {
            if (AAFileType.FTManifests == ft)
                return kGTManifestsURL;
            else if (AAFileType.FTSamples == ft)
                return kGTSamplesURL;
            else
                return kGTEventsURL;
        }

        public static bool tryToSend(AppAnalytics.MultipartUploader.FileParameter aFiles, Dictionary<string, List<object>> aToDel)
        {
            CallSequenceMonitor.logCall();
            if (NetworkInterface.GetIsNetworkAvailable() == true && aFiles.Count > 0 
                && aFiles.File != null && aToDel.Count > 0)
            {
                string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                string url = kGTBaseURL + typeToURL(aFiles.FileType) + Detector.getUDIDString();
                
                string contentType = "multipart/form-data; boundary=" + formDataBoundary;
                PUTRequest request = new PUTRequest(url, contentType);
                request.SetHeader("User-Agent", "Windows Phone");
               
                MultipartUploader.MultipartFormDataPut(request, aFiles, aToDel, formDataBoundary);

                return true;
            }
            
            return false;
        }

        public static void success(bool aSuccess, AAFileType aType, Dictionary<string, List<object> > aToDel)
        { 
            if (!aSuccess)
            {
                return;
            }

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
                try
                {
                    ManifestController.Instance.deletePackages(aToDel);
                }
                catch (Exception e)
                { Debug.WriteLine("handled " + e.ToString()); }
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

        #region sending

        public static bool sendSamplesDictAsBinary(Dictionary<string, List<byte[]>> aSamples, object _readLock)
        { 
            List<object> toDel = new List<object>();

            const int kMaxAtOnce = 1024 * 100; // 900 * 17
            int bts = 0;

            var files = new Dictionary<List<object>, MultipartUploader.FileParameter>();

            lock (_readLock)
            {
                foreach (var kval in aSamples)
                {
                    var ms = new MemoryStream();

                    ms.WriteByte((byte)'H');
                    ms.WriteByte((byte)'A');
                    ms.WriteByte(ManifestBuilder.kDataPackageFileVersion);

                    var session = Encoding.UTF8.GetBytes(kval.Key);
                    Debug.Assert(session.Length == 36); 

                    ms.Write(session, 0, session.Length);

                    var file = new MultipartUploader.FileParameter(ms.ToArray(), kval.Key, AAFileType.FTSamples);
                    ms.Dispose(); 

                    foreach (var byteArr in kval.Value)
                    {
                        bts += byteArr.Length;
                        toDel.Add(byteArr);

                        if (bts > kMaxAtOnce)
                        {
                            break;
                        } 
                        file.File = file.File.Concat(byteArr).ToArray(); 
                    }

                    files.Add(toDel ,file);
                    
                    toDel = new List<object>(); 
                }
            }

            foreach (var f in files)
            {
                Sender.tryToSend(f.Value, new Dictionary<string, List<object>> { { f.Value.FileName, f.Key } });
            }

            return true;
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
