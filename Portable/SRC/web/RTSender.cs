using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

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
        //private Mutex mLock = new Mutex();
        static List<string> mManifestToDel = new List<string>();
        static Dictionary<string, int> mPackagesToDel = new Dictionary<string, int>();

        public static bool tryToSend(Dictionary<string, object> aFiles, bool isItManfest, List<int> aHowMany = null)
        {
            return false; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (NetworkInterface.GetIsNetworkAvailable() == true && IsPreviousOperationComplete && aFiles.Count > 0)
            {
                if (isItManfest)
                {
                    bool success = MultipartUploader.MultipartFormDataPUT(kGTBaseURL +
                                                                           kGTManifestsURL 
                                                                            + Detector.getUDIDString(),
                                                                            "WindowsPhone",
                                                                            aFiles,
                                                                            isItManfest);
                    foreach (var it in aFiles)
                    {
                        mManifestToDel.Add(it.Key);
                    }
                }
                else
                {
                    bool success = MultipartUploader.MultipartFormDataPUT(kGTBaseURL +
                                                                           kGTSamplesURL
                                                                            + Detector.getUDIDString(),
                                                                            "WindowsPhone",
                                                                            aFiles,
                                                                            isItManfest);

                    int i = 0;
                    foreach (var it in aFiles)
                    {
                        if (i < aHowMany.Count)
                            mPackagesToDel[it.Key] = aHowMany[i];
                        i++;
                    }
                }

                return true;
            }
            else return false;
        }

        static public bool IsPreviousOperationComplete
        {
            get { return (mManifestToDel.Count + mPackagesToDel.Count) == 0; }
        }

        public static void success()
        {
            ManifestController.Instance.deleteManifests(mManifestToDel);
            ManifestController.Instance.deletePackages(mPackagesToDel);
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
