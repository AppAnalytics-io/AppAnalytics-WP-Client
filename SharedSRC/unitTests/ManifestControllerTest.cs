using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using AppAnalytics;
using System.IO;
 
namespace UTAppAnalytics
{
    [TestClass]
    public class ManifestControllerTest
    {
        [TestMethod]
        public void serializingDeserilizingSamplesChecker()
        {
            var mc = AppAnalytics.ManifestController.Instance;
            var tmp = new SerializableDictionary<string, List<byte[]>>();

            var et = new List<byte[]>() { new byte[]{1,2,3}, new byte[]{3,4,5} };

            tmp.Add("check", et);
            var copy =new SerializableDictionary<string, List<byte[]>>(tmp);

            MemoryStream ms = new MemoryStream();
            mc.serializeSamples(ms, tmp, false);
            ms.Seek(0, SeekOrigin.Begin);
            mc.deserializeSamples(ms, tmp);

            Assert.IsTrue(tmp.SequenceEqual(copy));
        }

        [TestMethod]
        public void serializingDeserilizingManifestsChecker()
        {
            var mc = AppAnalytics.ManifestController.Instance;
            var tmp = new Dictionary<string, byte[]>(); 

            tmp.Add("check", new byte[] { 3, 4, 5 });
            var copy = new Dictionary<string, byte[]>(tmp);

            MemoryStream ms = new MemoryStream();
            mc.serializeManifests(ms, tmp, false);
            ms.Seek(0, SeekOrigin.Begin);
            mc.deserializeManifests(ms, tmp); 

            Assert.IsTrue(tmp.SequenceEqual(copy));
        } 
    }
}
