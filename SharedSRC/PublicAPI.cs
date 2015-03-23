//#define TESTING

using System;
using System.Windows;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Diagnostics;


namespace AppAnalytics
{
    public static class API
    {
#if TESTING
        public static void PushTestSample(int id)
        {
            var enID = (GestureID) (id% ((int) GestureID.Navigation));
            GestureData gd = GestureData.create(enID, 
                                new Windows.Foundation.Point(42, 42), "testingE", "testingPage");

            ManifestController.Instance.buildDataPackage(gd);
        }

        static int mCounter = 0;
        public static void TestEventPushingTime()
        {
            mCounter++;
            var now = DateTime.Now;
            logEvent("testing : " + mCounter + " length:" + EventsManager.Instance.CurrentSessionEventsCount);
            var dif = now - DateTime.Now;

            Debug.WriteLine("time spend" + dif.TotalMilliseconds);
        }
#endif
        /// <summary>
        /// Init API with your application key (Length == 32)
        /// </summary>
        /// <param name="aApiKey"></param>
        public static void init(string aApiKey)
        {
            Detector.init(aApiKey);
        }
        /// <summary>
        /// Register an event
        /// </summary>
        /// <param name="aDescription">Name/description of an event</param>
        public static void logEvent(String aDescription)
        {
            EventsManager.Instance.pushEvent(aDescription);
        }
        /// <summary>
        /// Register an event
        /// </summary>
        /// <param name="aDescription">Name/description of an event</param>
        /// <param name="aParameters">Additional parameters as key/value pair</param>
        public static void logEvent(String aDescription, Dictionary<string, string> aParameters)
        {
            EventsManager.Instance.pushEvent(aDescription, aParameters);
        }
        /// <summary>
        /// Enable or disable debug log output
        /// </summary>
        public static bool DebugLogEnabled
        {
            set { EventsManager.Instance.DebugLogEnabled = value; }
            get { return EventsManager.Instance.DebugLogEnabled; }
        }
        /// <summary>
        /// Enable or disable exception analytics
        /// </summary>
        public static bool ExceptionAnaluticsEnabled
        {
            set { EventsManager.Instance.ExceptionAnalyticsEnabled = value; }
            get { return EventsManager.Instance.ExceptionAnalyticsEnabled; }
        }
        /// <summary>
        /// Enable or disable screen analytics
        /// </summary>
        public static bool ScreenAnaluticsEnabled
        {
            set { EventsManager.Instance.ScreenAnalitycsEnabled = value; }
            get { return EventsManager.Instance.ScreenAnalitycsEnabled; }
        }
        /// <summary>
        /// Gets or sets event dispatch interval in seconds (max - 3600, min - 10, def - 120)
        /// </summary>
        public static float DispatchInterval
        {
            set { EventsManager.Instance.DispatchInterval = value; }
            get { return EventsManager.Instance.DispatchInterval; }
        }
        /// <summary>
        /// Enable or disable transactions analytics (currently unavailable)
        /// </summary>
        public static bool TransactionAnalyticsEnabled
        {
            set { EventsManager.Instance.TransactionAnaliticsEnabled = value; }
            get { return EventsManager.Instance.TransactionAnaliticsEnabled; }
        }
        /// <summary>
        /// Tracking purchases. Use this function in your transaction callbacks.
        /// </summary>
        /// <param name="aProductID">product identifier as string</param>
        /// <param name="aState">current state of transaction</param>
        public static void trackTransaction(string aProductID, TransactionState aState)
        {
            TransactionAPI.handleTransaction(aProductID, aState);
        }
    }
}
