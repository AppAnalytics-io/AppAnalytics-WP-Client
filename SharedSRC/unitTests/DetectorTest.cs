using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.IO;
using AppAnalytics;

namespace UTAppAnalytics
{
    internal class MyMockMC : AppAnalytics.IManifestController
    {
        //public sendManifestW
        public void loadData() { }
        public void store() { }

        public bool deserializeManifests(Stream stream, Dictionary<string, byte[]> container) { return true; }

        public bool deserializeSamples(Stream stream,
                            SerializableDictionary<string, List<byte[]>> container){return true;}


        public bool serializeManifests(Stream stream, Dictionary<string, byte[]> container, bool dispose = true) { return true; }

        public bool serializeSamples(Stream stream,
                            SerializableDictionary<string, List<byte[]>> container, bool dispose = true){return true;}

        public void buildDataPackage(AppAnalytics.GestureData aData)
        {
        }

        public void buildSessionManifest() { }

        public bool sendManifest()
        {
            return true;
        }

        public bool sendSamples() { return true; }

        public void deleteManifests(List<string> list) { }

        public void deletePackages(Dictionary<string, List<object>> map) { }

        public int SamplesCount
        {
            get {return 0;}
        }
    }

    [TestClass]
    public class DetectorTester
    {

        [ClassInitialize()]
        public static void ClassInitialize(TestContext context)
        {
            try
            {
                AppAnalytics.Detector.init("1", true);
                Assert.IsTrue(false);
            } 
            catch (Exception)
            {
                //It is fine. should threw an expt if key is wrong
            }
            Assert.IsFalse(AppAnalytics.Detector.IsInitialized,
                "IsInitialized should be false after init with wrong key");
        }   

        [TestMethod]
        public void InitializtingDetetorTwiceShouldNotThrewAnException()
        {
            try
            {
//                 var t = new Moq.Mock<aaa>(); 
                AppAnalytics.Detector.setupControllers(new MyMockMC());

                AppAnalytics.Detector.init("2miKqKyeGhoQgvIImX9UfAf17fuwnyvP", true);
                Assert.IsTrue(AppAnalytics.Detector.IsInitialized, "IsInitialized should be true after init with right key");
                AppAnalytics.Detector.init("2miKqKyeGhoQgvIImX9UfAf17fuwnyvP", true);
            }
            catch (Exception e) { Assert.IsFalse(true); }
            var flag = AppAnalytics.CallSequenceMonitor.isInSequence("sendManifest");

            Assert.IsTrue(flag, "We should send a manifest in init method");
        }

//         [TestMethod] Current Window is null int UT project so seems like we cannot use this test
//         public void ResoulutionGettersShouldReturnPositiveNonZeroValue()
//         {
//             var x =     AppAnalytics.Detector.getResolutionXDouble();
//             var y =     AppAnalytics.Detector.getResolutionYDouble();
//             var xb =    AppAnalytics.Detector.getResolutionX();
//             var yb =    AppAnalytics.Detector.getResolutionY();
// 
//             Assert.IsInstanceOfType(x, typeof(double));
//             Assert.IsInstanceOfType(y, typeof(double));
//             Assert.IsInstanceOfType(xb, typeof(byte[]));
//             Assert.IsInstanceOfType(yb, typeof(byte[]));
// 
//             Assert.IsTrue(xb.Length == sizeof(double));
//             Assert.IsTrue(yb.Length == sizeof(double));
// 
//             Assert.IsTrue((x * y) != 0, "x and y cannot be equal to zero");
//             Assert.IsTrue((x * y) > 0, "x and y cannot be less than zero");
//         }

//         [TestMethod]
//         public void ManifestShou()
//         {
//             Assert.IsTrue(AppAnalytics.Detector.getSessionID().Length == 36);
//         } 

        [TestMethod]
        public void SessionIDShouldBe36BytesLong()
        {
            Assert.IsTrue(AppAnalytics.Detector.getSessionID().Length == 36);
        }

        [TestMethod]
        public void SystemLocaleSholdBe3ByteLong()
        {
            Assert.IsTrue(AppAnalytics.Detector.SystemLocale.Length == 3);
        }

        [TestMethod]
        public void OSVersionShouldBe16BytesLong()
        {
            Assert.IsTrue(AppAnalytics.Detector.OSVersion.Length == 16);
        }

        [TestMethod]
        public void AppVersionShouldBe16BytesLong()
        {
            Assert.IsTrue(AppAnalytics.Detector.AppVersion.Length == 16);
        }

        [TestMethod]
        public void GetUDID32ShouldBe32BytesLong()
        {
            Assert.IsTrue(AppAnalytics.Detector.getUDID32().Length == 32);
        }

        [TestMethod]
        public void GetUDIDStringShouldBeWithNoDashes()
        {
            Assert.IsTrue(AppAnalytics.Detector.getUDIDString().Contains("-") == false);
        }
    }
}
