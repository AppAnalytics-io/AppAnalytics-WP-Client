using System;
using System.Collections.Generic;
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

        static List<string> mManifestToDel = new List<string>();
        static Dictionary<string, List<object>> mPackagesToDel = new Dictionary<string, List<object>>();
        static Dictionary<string, List<object>> mEventsToDel = new Dictionary<string, List<object>>();


        public static bool tryToSend(AppAnalytics.MultipartUploader.FileParameter aFiles, List<object> aToDel)
        {
            if (NetworkInterface.GetIsNetworkAvailable() == true && aFiles.Count > 0)
            { 
                bool success = MultipartUploader.MultipartFormDataPut(kGTBaseURL +
                                                                        kGTManifestsURL
                                                                        + Detector.getUDIDString(),
                                                                        "WindowsPhone",
                                                                        aFiles
                                                                        );
                // handling recycle  
                if (AAFileType.FTManifests != aFiles.FileType)
                {
                    mManifestToDel.Add(aFiles.FileName);
                }
                else if (AAFileType.FTSamples != aFiles.FileType)
                {
                    addRangeToDelList(aFiles.FileName, aToDel, mPackagesToDel); 
                }
                else if (AAFileType.FTEvents != aFiles.FileType)
                { 
                    addRangeToDelList(aFiles.FileName, aToDel, mEventsToDel); 
                }  

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

        static public bool IsPreviousOperationComplete
        {
            get { return (mManifestToDel.Count + mPackagesToDel.Count) == 0; }
        }

        public static void success(AAFileType aType)
        {
            if (aType == AAFileType.FTManifests)
            {
                ManifestController.Instance.deleteManifests(mManifestToDel);
            }
            else if (aType == AAFileType.FTSamples)
            {
                ManifestController.Instance.deletePackages(mPackagesToDel);
            }
            else if (aType == AAFileType.FTEvents)
            {
                EventsManager.Instance.deleteEvents(mEventsToDel);
                //throw new NotImplementedException();
                //ManifestController.Instance.dele
            }

            lock (_lockObj)
            {
                mManifestToDel.Clear();
                mPackagesToDel.Clear();
            }
            if (kTryToSendMore && (ManifestController.Instance.SamplesCount > 10) )
            {
                ManifestController.Instance.sendSamples();
            }
        }

        public static void fail()
        {
            lock (_lockObj)
            {
                mManifestToDel.Clear();
                mPackagesToDel.Clear();
            }
        }
    }
}
