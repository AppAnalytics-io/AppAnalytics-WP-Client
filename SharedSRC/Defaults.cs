using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace AppAnalytics
{
    internal enum AAFileType
    {
        FTManifests,
        FTSamples,
        FTEvents
    }

    static internal class Defaults
    {
        public const UInt32 kMaxLogEventStrLen   = 512;
        public const UInt32 kMaxDispatchInterval = 3600;
        public const UInt32 kMinDispatchInterval = 10;
        public const UInt32 kDefDispatchInterval = 120;

        public const UInt32 kMaxPacketSize = 1024*100;

        public const bool kDbgLogEnabled = false;
        public const bool kExceptionAnalyticsEnabled = true;
        public const bool kTransactionAnalyticsEnabled = true;
        public const bool kScreensAnalyticsEnabled = false;

        // use it to avoid possible name conflict
        // (if client app have same names)
        // just add like this @file_name
        public const string kFileExpKey = ".aa_sff";

        public const string kStrNull = "Null";

        public struct ExceptionTxt
        {
            public const string kStrEventName = "Uncaught Exception";
            public const string kStrReason = "Reason";
            public const string kStrType = "Name";
            public const string kStrCallStack = "Call Stack Trace";
        }

        public struct NavigationTxt
        {
            public const string kStrEventName = "Navigation";
            public const string kStrType = "Screen Class Name";
            public const string kStrMode = "Navigation Mode";
            public const string kStrSource = "Source";
            public const string kStrDestination = "Destination";
        }

        public struct PaymentTxt
        { 
            public const string kTransactionEventType = "Type";
            public const string kTransactionEventId = "Identifier";
            public const string kTransactionStatePurchasing = "Transaction Initiated";
            public const string kTransactionStateSucceeded = "Transaction Succeeded";
            public const string kTransactionStateFailed = "Transaction Failed";
            public const string kTransactionStateRestored = "Transaction Restored";
            public const string kTransactionStateDeferred = "Transaction Deferred";

            public const string kStrEventName = "Transaction";

        }
    }
}
