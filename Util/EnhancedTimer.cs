using System;
using System.Timers;

namespace PropHunt.Util
{
    // Taken from https://stackoverflow.com/a/19382455
    internal class EnhancedTimer : Timer
    {
        private DateTime _dueTime;
        public double TimeLeft => (_dueTime - DateTime.Now).TotalMilliseconds;

        public EnhancedTimer() => Elapsed += OnElapsed;

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            if (AutoReset)
            {
                _dueTime = DateTime.Now.AddMilliseconds(Interval);
            }
        }
        protected new void Dispose()
        {
            Elapsed -= OnElapsed;
            base.Dispose();
        }

        public new void Start()
        {
            _dueTime = DateTime.Now.AddMilliseconds(Interval);
            base.Start();
        }
    }
}
