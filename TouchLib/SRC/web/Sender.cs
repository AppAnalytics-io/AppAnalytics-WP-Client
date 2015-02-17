using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Phone.Net.NetworkInformation;

namespace TouchLib
{
    internal static class Sender
    {
        public const string kGTBaseURL      = "http://www.appanalytics.io/api/v1/"; // @"http://192.168.1.36:6249/api/v1";
        public const string kGTManifestsURL = "manifests?UDID=";
        public const string kGTSamplesURL = "samples?UDID=";
        //private Mutex mLock = new Mutex();
        static List<string> mManifestToDel = new List<string>();
        static List<string> mPackagesToDel = new List<string>();

        public static bool tryToSend(Dictionary<string, object> aFiles, bool isItManfest)
        {
            if (NetworkInterface.GetIsNetworkAvailable() == true && IsPreviousOperationComplete)
            {
                if (isItManfest)
                {
                    bool success = MultipartUploader.MultipartFormDataPost(kGTBaseURL +
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
//                     Dictionary<string, object> tmp;
//                     List<byte> total;
//                     foreach ()

                    bool success = MultipartUploader.MultipartFormDataPost(kGTBaseURL +
                                                                           kGTSamplesURL
                                                                            + Detector.getUDIDString(),
                                                                            "WindowsPhone",
                                                                            aFiles,
                                                                            isItManfest);
                    foreach (var it in aFiles)
                    {
                        mPackagesToDel.Add(it.Key);
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

            mManifestToDel.Clear();
            mPackagesToDel.Clear();
        }

        public static void fail()
        {
            mManifestToDel.Clear();
            mPackagesToDel.Clear();
        }

        public static void deletePackagesFromStorage()
        {

        }
    }
}
