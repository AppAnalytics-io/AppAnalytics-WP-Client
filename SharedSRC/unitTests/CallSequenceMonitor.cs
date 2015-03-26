using System;
using System.Windows;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections;

namespace AppAnalytics
{
    //This class should be used only for testing
    // this way we don't need a thread ID in CallInfo

    // TEMP -> TODO find moq analogue for WP8.1 RT

    internal static class CallSequenceMonitor
    {
        public class CallInfo 
        {
            public string FuncName = "";
            public DateTime Date = new DateTime();
            public CallInfo(string name)
            {
                FuncName = name;
                Date = DateTime.Now;
            }
        }

        private static readonly object _locker = new object();
        static List<CallInfo> mLoggedCalls = new List<CallInfo>();


        public static void logCall([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {    
#if DEBUG
            lock (_locker)
            {
                if (mLoggedCalls.Count > 10)
                {
                    mLoggedCalls.Remove(mLoggedCalls.FirstOrDefault());
                }
                if ("" != memberName)
                    mLoggedCalls.Add(new CallInfo(memberName));
            }
#endif
        }

#if DEBUG
        public static void clear()
        {
            lock (_locker)
            {
                mLoggedCalls.Clear();
            }
        }
        public static CallInfo getLastLogged()
        {
            lock (_locker)
            {
                if (mLoggedCalls.Count == 0)
                    return null;

                return mLoggedCalls.Last();
            }
        }

        public static CallInfo getLogged(int Index)
        {
            lock (_locker)
            {
                if (mLoggedCalls.Count < Index)
                    return null;

                return mLoggedCalls[Index];
            }
        }

        public static bool isInSequence(string aMethodName)
        {
            lock (_locker)
            {
                foreach (var item in mLoggedCalls)
                {
                    if (item.FuncName == aMethodName)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static bool assertSequence(List<string> aSequence)
        {
            lock (_locker)
            {
                if (aSequence.Count > mLoggedCalls.Count)
                {
                    return false;
                }

                int i = aSequence.Count - 1;
                foreach (var item in (mLoggedCalls as IEnumerable<CallInfo>).Reverse())
                {
                    if (item.FuncName != aSequence[i])
                    {
                        return false;
                    }
                    i--;
                }

                return true;
            }
        }

#endif
    }
}
