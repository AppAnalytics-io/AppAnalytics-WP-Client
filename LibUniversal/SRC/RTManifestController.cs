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
using System.Net.NetworkInformation;

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

       // AutoResetEvent mSyncEvent = new AutoResetEvent(false);

        override public void loadData()
        {
            loadDataAsync().Wait();
        }

        public async Task loadDataAsync()
        {
            var fHelper = new FileSystemHelper();
            var tmpSmpl = new SerializableDictionary<string, List<byte[]>>();
            var tmpMan = new Dictionary<string, byte[]>();

            try
            {
                Stream stream = await fHelper.getFileStreamAsync("manifests" + Defaults.kFileExpKey, true);

                if (null != stream)
                {
                    deserializeManifests(stream, tmpMan);
                }

                stream = await fHelper.getFileStreamAsync("samples" + Defaults.kFileExpKey, true);
                if (null != stream)
                {
                    deserializeSamples(stream, tmpSmpl);

                    stream.Dispose();
                }
            }
            catch (Exception)
            {
                //Debug.WriteLine(e.Message + "\n Cannot create file.");
            }

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
            //mSyncEvent.Set();
        }

        protected ManifestController()
        {
            //mSyncEvent.Reset();
            //
            Task load = new Task(loadData);
            load.Start(); // maybe we can @fire and forget@ about this. will think about it.
            load.Wait();
        }

        ~ManifestController()
        {
            var tmp = new Task(store);
            tmp.Start();
            tmp.Wait(); // storing samples that are not sent yet.
        } 

        override public async void store()
        {
            var fh = new FileSystemHelper();
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

                var stream = await fh.getFileStreamAsync("manifests" + Defaults.kFileExpKey, false);
                if (tmpMan.Count > 0 && null != stream)
                {
                    serializeManifests(stream, tmpMan);
                }
                else
                {
                    fh.deleteFile("manifests" + Defaults.kFileExpKey); 
                }

                stream = await fh.getFileStreamAsync("samples" + Defaults.kFileExpKey, false);
                if (mSamples.Count > 0 && null != stream)
                {
                    serializeSamples(stream, tmpSmpl);
                }
                else
                {
                    fh.deleteFile("samples" + Defaults.kFileExpKey); 
                }
            }
            catch 
            {
                //Debug.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }
        } 
    }
}
