using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace TAppAnalytics
{
    public static class Simulation
    {
        public static async void CrashApp(TimeSpan aDelay)
        {
            await Task.Delay(aDelay);
            throw new NullReferenceException();
        }

        public static void CheckInsertionTime()
        {
            AppAnalytics.API.TestEventPushingTime();
        }
    }
}
