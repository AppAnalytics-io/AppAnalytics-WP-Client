using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Diagnostics; 

namespace TAppAnalytics
{
    public static class BigDataSimulation
    {
        public static void PushRandomEvents(UInt16 aHowMany)
        {
            AppAnalytics.API.DispatchInterval = 100;

            var now = DateTime.Now; 
            for (int i = 0; i < aHowMany; ++i)
            {
                if (i % 10 == 0)
                {
                    AppAnalytics.API.logEvent(i.ToString(),
                        new Dictionary<string, string> { {"key1", "val1"}, {"key2", "val2"} });
                }
                else if ( i % 3 == 0)
                {
                    AppAnalytics.API.logEvent("some_Event_type2",
                        new Dictionary<string, string> { { "key1", "val1" } });
                }
                else
                {
                    AppAnalytics.API.logEvent("some_Event_type3",
                        new Dictionary<string, string> { { "key1", "val1" } });
                }
            }
             var dif = DateTime.Now - now;

            Debug.WriteLine("Inserting "+ aHowMany +" events. time spend" + dif.TotalMilliseconds); 
        } 
        // make sure, that TESTING is defined in PublicAPI class to test it
        public static void PushRandomSamples(UInt16 aCount)
        { 
            for (int i = 0; i < aCount; ++i)
            {
                AppAnalytics.API.PushTestSample(i);
            }
        }
    } 
}
