using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TouchLib
{
    internal class Sender
    {
        const string kGTBaseURL      = "http://www.appanalytics.io/api/v1"; // @"http://192.168.1.36:6249/api/v1";
        const string kGTManifestsURL = "manifests";
        const string kGTSamplesURL   = "samples";
        //private Mutex mLock = new Mutex();
        private Queue<String> mMessagesToSend = new Queue<string>();

        public void addMessageToSend( string aMessage )
        {
            lock (mMessagesToSend)
            {
                mMessagesToSend.Enqueue (aMessage);
            }
        }

        private string popMessage( )
        {
            string val;
            lock (mMessagesToSend)
            {
                val = mMessagesToSend.Dequeue();
            }

            return val;
        }

        public void sendMessage()
        {
            // pop from queue

            // wrap

            // send (somehow)
        }
    }
}
