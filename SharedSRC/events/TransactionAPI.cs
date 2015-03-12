using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AppAnalytics
{
    public enum TransactionState
    {
        Purchasing,
        Purchased,
        Failed,
        Restored,
        Deferred
    }

    internal static class TransactionAPI
    {
        public static void handleTransaction(string aProductID, TransactionState aState)
        {
            if (!EventsManager.Instance.TransactionAnaliticsEnabled)
            {
                Debug.WriteLine("Refuse: Transaction analytics disabled. Enable it using API.");
                return;
            }

            var info = new Dictionary<string, string>() 
            { 
                {Defaults.PaymentTxt.kTransactionEventId, aProductID == "" ? Defaults.kStrNull : aProductID } 
            };
            string key = Defaults.PaymentTxt.kTransactionEventType;

            switch (aState)
            {
                case TransactionState.Deferred:
                    info.Add(key, Defaults.PaymentTxt.kTransactionStateDeferred);
                    break;
                case TransactionState.Failed:
                    info.Add(key, Defaults.PaymentTxt.kTransactionStateFailed);
                    break;
                case TransactionState.Purchased:
                    info.Add(key, Defaults.PaymentTxt.kTransactionStateSucceeded);
                    break;
                case TransactionState.Purchasing:
                    info.Add(key, Defaults.PaymentTxt.kTransactionStatePurchasing);
                    break;
                case TransactionState.Restored:
                    info.Add(key, Defaults.PaymentTxt.kTransactionStateRestored);
                    break;
            }

            EventsManager.Instance.pushEvent(Defaults.PaymentTxt.kStrEventName, info);
        }
    }

}
