using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using AppAnalytics;

//System.Reflection.MethodBase.GetCurrentMethod().Name
namespace UTAppAnalytics
{
    using ManifestsDict = Dictionary<string, byte[]>;
    using SamplesDIct = SerializableDictionary<string, List<byte[]>>;

    [TestClass]
    public class BuliderChecker
    {
        [TestMethod]
        public void dataPackageShouldMatchLengthAndBeAdded()
        {
            var obj = new object();
        	//arrange
        	GestureData gd  = GestureData.createTestSample();
            int length = GestureData.testSampleSize();
            SamplesDIct sd = new SamplesDIct();

            //action
            ManifestBuilder.buildDataPackage(gd, sd, obj);

            //assert
            Assert.IsTrue(sd.Count == 1, "Package should be added to dictionary");
            Assert.IsTrue(sd.FirstOrDefault().Value[0].Length == length,
                "Package length should match expected value");
        }

        [TestMethod]
        public void sessionManifestLenghtShouldMatch171()
        {
            var obj = new object();
            ManifestsDict md = new ManifestsDict();

            ManifestBuilder.buildSessionManifest(md, obj);
            
            //assert
            Assert.IsTrue(md.Count == 1, "Manifest should be added to dictionary");
            Assert.IsTrue(md.FirstOrDefault().Value.Length == 171,
                "Manifest length should match expected value");
        }
    }
}
