using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UTAppAnalytics
{
    [TestClass]
    public class EventsManagerTester
    {
        [TestMethod] 
        public void indexShouldChangingIfEventIsAdded()
        {
            var before = AppAnalytics.EventsManager.Instance.Index;
            AppAnalytics.EventsManager.Instance.pushEvent("index++", null);
            AppAnalytics.EventsManager.Instance.insertEvents();
            var after = AppAnalytics.EventsManager.Instance.Index;

            Assert.IsTrue(before != after);
        }

        [TestMethod]
        public void indexShouldNotChangingIfInsertIsNotCalled()
        {
            var before = AppAnalytics.EventsManager.Instance.Index;
            AppAnalytics.EventsManager.Instance.pushEvent("index++", null); 
            var after = AppAnalytics.EventsManager.Instance.Index;

            Assert.IsFalse(before != after);
        }

        [TestMethod]
        public void testingEventsCleanUp()
        {
            var ae = AppAnalytics.EventsManager.Instance.pushEvent("del", null);
            AppAnalytics.EventsManager.Instance.insertEvents();

            int cnt = AppAnalytics.EventsManager.Instance.CurrentSessionEventsCount;

            AppAnalytics.EventsManager.Instance.deleteEvents(
                  new Dictionary<string, List<object>>()
                  {
                  {AppAnalytics.Detector.getSessionIDStringWithDashes(),
                      new List<object>(){ae}}
                  }
                );

            Assert.IsTrue(cnt > AppAnalytics.EventsManager.Instance.CurrentSessionEventsCount,
                "seems like event was not deleted");
        }  
    }
}
