using System;
using System.Diagnostics;
using System.Threading;

namespace Services
{
    // count CPU (total) usage by app
    public class CpuCounterService : IService
    {
        PerformanceCounter _counter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private Timer _timer;
        private int _period = 600;
        public double Value { get; private set; } = 0;
        
        public CpuCounterService()
        {
        }

        public void OnStart()
        {
            Thread.BeginThreadAffinity();
            _timer = new Timer(NextValue, null, 0, 600);
        }

        public void OnStop()
        {
            //_timer.Change(Timeout.Infinite, _period);
            _timer.Dispose();
        }
        
        public void NextValue(object state)
        {
            _counter.NextValue();
            // todo:
            Thread.Sleep(500);
            Value = _counter.NextValue();
        }
    }
}