using System.Collections.Generic;
using System.Threading;
using VitualPersonSpeech.Tasks;

namespace VitualPersonSpeech.Utils
{
    public class HandleMsgTask : Task
    {
        private ManualResetEvent mre = new ManualResetEvent(false);
        private Queue<string> clientMsgs = new Queue<string>();
        public delegate void OnMessage(object sender, string message);
        public OnMessage onMessage;

        public HandleMsgTask(OnMessage onMessage)
        {
            this.onMessage += onMessage;
        }

        public override void doWork()
        {
            mre.WaitOne();

            while (!base.stop && clientMsgs.Count > 0)
            {
                string udpMsg;
                lock (clientMsgs)
                {
                    udpMsg = clientMsgs.Dequeue();
                }
                onMessage(this, udpMsg);
            }
        }

        public void AddClientMsg(string udpMsg)
        {
            lock (clientMsgs)
            {
                clientMsgs.Enqueue(udpMsg);
            }
            mre.Set();
        }
    }
}
