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
        public const UInt32 kMinDispatchInterval = 3600;
        public const UInt32 kDefDispatchInterval = 120;

        public const UInt32 kMaxPacketSize = 1024*100;

        public const bool kDbgLogEnabled = false;
        public const bool kExceptionAnalyticsEnabled = true;
        public const bool kTransactionAnalyticsEnabled = true;
        public const bool kScreensAnalyticsEnabled = false;

        public const string kFileExpKey = "aa_sff";
    }
}
