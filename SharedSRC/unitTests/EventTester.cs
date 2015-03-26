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
    public class EventTester
    {
        [TestMethod] 
        public void eventsJSONRepresentationShouldBeValid()
        {
            bool flag = true;
            var ev = AppAnalytics.AAEvent.create(0,0,"texttext", null);
            string jsonString = ev.getJsonString();

            try
            {
                var obj = JToken.Parse(jsonString);
            }
            catch (JsonReaderException ex)
            {
                flag = false;//Exception in parsing json 
            }
            catch (Exception) //some other exception
            { 
                flag = false;
            }

            Assert.IsTrue(flag, "JSON string should be valid");
        }

        [TestMethod]
        public void eventsShouldBeEqualIfTheHaveSameParametersAndDesc()
        {
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;

            var ev0 = AppAnalytics.AAEvent.create(0, 0, "texttext", null);
            var ev1 = AppAnalytics.AAEvent.create(0, 1, "texttext", null);

            var ev2 = AppAnalytics.AAEvent.create(0, 0,
                            "texttext", new Dictionary<string, string>() { { "k1", "v1" } });
            var ev3 = AppAnalytics.AAEvent.create(0, 1,
                            "texttext", new Dictionary<string, string>() { { "k1", "v1" } });

            flag1 = ev0.Equals(ev1);
            flag2 = ev2.Equals(ev3);
            flag3 = ev0.Equals(ev3);

            Assert.IsTrue(flag1, "Events with same description and no params "
                                   + "should be equal");
            Assert.IsTrue(flag2, "Events with same description and params "
                                   + "should be equal");
            Assert.IsFalse(flag3, "Events with same description and different params"
                                    +" should be not equal.");
        }

        [TestMethod]
        public void toStringShouldBeOverriden()
        {
            var ev = AppAnalytics.AAEvent.create(0,0,"texttext", null);
            string test = ev.ToString();

            Assert.IsTrue(test.Contains(ev.Description)
                , "ToString is not overridden or does not contains event description.");
        }
    }
}
