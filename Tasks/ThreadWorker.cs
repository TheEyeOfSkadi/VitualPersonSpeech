using System.Threading;

namespace VitualPersonSpeech.Tasks
{
    public class ThreadWorker
    {
        protected Task task;
        protected Thread workerThread;
        protected bool exit = false;

        public ThreadWorker(Task task)
        {
            this.task = task;
        }

        public bool Start()
        {
            if (workerThread != null)
                return false;

            // 启动网络监听线程
            workerThread = new Thread(Run);
            workerThread.IsBackground = true;
            workerThread.Start();

            return true;
        }

        private void Run()
        {
            while (!exit)
                task.doWork();
        }

        public virtual void Stop()
        {
            if (workerThread != null)
            {
                exit = true;
                task.Stop();
                if (!workerThread.Join(3000))
                    workerThread.Abort();
            }

            task.CleanUp();
        }
    }
}
