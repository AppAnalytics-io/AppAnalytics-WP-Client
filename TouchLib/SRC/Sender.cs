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
