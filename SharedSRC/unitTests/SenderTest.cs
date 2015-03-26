using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace UTAppAnalytics
{
    [TestClass]
    public class SenderTester
    {
        [TestMethod] 
        public void tryToSendShouldReturnFalseIfThereIsEmptyFileToSend()
        {
            var param = new AppAnalytics.MultipartUploader.FileParameter(null, "", AppAnalytics.AAFileType.FTEvents);

            Assert.IsFalse(AppAnalytics.Sender.tryToSend(param, new Dictionary<string, List<object>>() { {"", null} }));
        }

        [TestMethod]
        public void tryToSendShouldReturnFalseIfThereIsNoFileToDelete()
        {
            var param = new AppAnalytics.MultipartUploader.FileParameter(new byte[]{1,1,1}, "", AppAnalytics.AAFileType.FTEvents);

            Assert.IsFalse(AppAnalytics.Sender.tryToSend(param, new Dictionary<string, List<object>>()));
        }

        [TestMethod]
        public void SenderShouldDeleteEventsOnSuccess()
        {
            var before = AppAnalytics.EventsManager.Instance.CurrentSessionEventsCount;

            var tst = AppAnalytics.EventsManager.Instance.pushEvent("event");
            AppAnalytics.EventsManager.Instance.insertEvents();

            var dct = new Dictionary<string, List<object>>()
            {  
                {AppAnalytics.Detector.getSessionIDStringWithDashes(), 
                new List<object>(){tst}}
            };
            AppAnalytics.Sender.success(true, AppAnalytics.AAFileType.FTEvents, dct);

            var after = AppAnalytics.EventsManager.Instance.CurrentSessionEventsCount;

            Assert.IsTrue(after == before,"event should be deleted after simulation");
        }

        [TestMethod]
        public void SenderShouldDeleteManifestsOnSuccess()
        {
            AppAnalytics.ManifestController.Instance.buildSessionManifest(); ;
            var dct = new Dictionary<string, List<object>>()
            {  
                {AppAnalytics.Detector.getSessionIDString(),  null}
            };
            AppAnalytics.Sender.success(true, AppAnalytics.AAFileType.FTManifests, dct);

            Assert.IsFalse(AppAnalytics.ManifestController.Instance.ContainsCurrentManifest,
                            "manifest should be deleted after simulation");
        }
         
    }
}
