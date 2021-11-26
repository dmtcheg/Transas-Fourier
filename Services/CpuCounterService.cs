using System;
using System.Diagnostics;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Services
{
    // count CPU (total) usage by app
    public class CpuCounterService : IService
    {
        static PerformanceCounter _counter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private Timer _timer;
        public double Value { get; private set; } = 0;
        
        public CpuCounterService()
        {
            _timer = new Timer(600);
            _timer.Elapsed += NextValue;
        }

        public void OnStart()
        {
            Thread.BeginThreadAffinity();
            _timer.Enabled = true;
        }

        public void OnStop()
        {
            _timer.Enabled = false;
        }

        public void Init()
        {
            var thread = new Thread(this.OnStart);
            thread.IsBackground = true;
            thread.Start();
        }
        
        public void NextValue(object sender, EventArgs e)
        {
            _counter.NextValue();
            // todo:
            Thread.Sleep(500);
            Value = _counter.NextValue();
        }
    }
}