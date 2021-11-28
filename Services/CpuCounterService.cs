using System;
using System.Diagnostics;
using System.Threading;
using NickStrupat;

namespace Services
{
    // count CPU (total) usage by app
    public class CpuCounterService : IService
    {
        private PerformanceCounter _counter;
        private ComputerInfo _info;
        private Timer _timer;
        private int _period = 600;
        public double Value { get; private set; } = 0;
        public double RamValue { get; set; } = 0;
        
        public CpuCounterService()
        {
            _counter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            _info = new ComputerInfo();
        }

        public void OnStart()
        {
            Thread.BeginThreadAffinity();
            var callback = new TimerCallback((state) =>
            {
                NextValue();
                CurrentMemoryLoad();
            });
            _timer = new Timer(callback, null, 0, 600);
        }

        public void OnStop()
        {
            //_timer.Change(Timeout.Infinite, _period);
            _timer.Dispose();
        }
        
        public void NextValue()
        {
            _counter.NextValue();
            Thread.Sleep(500);
            Value = _counter.NextValue()/Environment.ProcessorCount;
        }
        
        public void CurrentMemoryLoad()
        {
            RamValue =  100 * Environment.WorkingSet / (long)_info.TotalPhysicalMemory;
        }
    }
}