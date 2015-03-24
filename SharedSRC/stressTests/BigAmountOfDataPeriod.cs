using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Threading;

namespace TAppAnalytics
{
    public static class BigDataSimulationPeriodical
    {
        private static UInt16 aHowManyEvents = 10;
        private static UInt16 aHowManySamples = 10;

        private static Timer mSendingTimer = null;
        private static Timer mSendingSamplesTimer = null;

        public static void PushRandowEventsWithPeriod(UInt16 aHowMany, int aSecInterval)
        {
            aHowManyEvents = aHowMany;
            mSendingTimer = new Timer(PushRandomEvents, null, 0, (aSecInterval * 1000));
        }

        public static void PushRandomSamplesWithPeriod(UInt16 aHowMany, int aSecInterval)
        {
            aHowManySamples = aHowMany;
            mSendingSamplesTimer = new Timer(PushRandomEvents, null, 0, (aSecInterval * 1000));
        }

        private static async void PushRandomEvents( Object obj )
        {
            //AppAnalytics.API.DispatchInterval = 11;
            for (int i = 0; i < aHowManyEvents; ++i)
            {
                if (i % 5 == 0)
                {
                    AppAnalytics.API.logEvent("some_Event_type1",
                        new Dictionary<string, string> { { "key1", "val1" }, { "key2", "val2" } });
                }
                else if (i % 3 == 0)
                {
                    AppAnalytics.API.logEvent("some_Event_type2",
                        new Dictionary<string, string> { { "key1", "val1" } });
                }
                else
                {
                    AppAnalytics.API.logEvent(i.ToString());
                }
            }
        }

        // make sure, that TESTING is defined in PublicAPI class to test it
        private static async void PushRandomSamples( Object obj )
        {
            for (int i = 0; i < aHowManySamples; ++i)
            {
                AppAnalytics.API.PushTestSample(i);
            }
        }
    }
}
