namespace VitualPersonSpeech.Tasks
{
    public abstract class Task
    {
        public bool stop { get; set; }

        public abstract void doWork();

        public void Stop()
        {
            stop = true;
            doStop();
        }

        protected virtual void doStop() { }

        public virtual void CleanUp() { }
    }
}
