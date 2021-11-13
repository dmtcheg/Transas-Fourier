using System;
using System.Collections.Generic;
using System.Diagnostics;
using NickStrupat;
using OxyPlot;
using System.Linq;
using System.Timers;
using System.Windows.Threading;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace FourierTransas
{
    public class MonitorService
    {
        private Timer _timer;
        PerformanceCounter _cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private ComputerInfo _info = new ComputerInfo();
        private List<DataPoint> _cpuSamples;
        private List<DataPoint> _ramSamples;
        private List<List<DataPoint>> _threadSamples;
        public PlotModel LoadModel { get; private set; }
        public PlotModel ThreadModel { get; private set; }
        public PlotModel RamModel { get; private set; }

        public double CurrentCpuLoad => _cpuSamples[_cpuSamples.Count - 1].Y;
        public double CurrentMemoryLoad => _ramSamples[_ramSamples.Count - 1].Y;

        public MonitorService()
        {
            _cpuSamples = new List<DataPoint>();
            _ramSamples = new List<DataPoint>();
            _threadSamples = new List<List<DataPoint>>();


            _timer = new Timer(200);
            _timer.AutoReset = true;
            _timer.Elapsed += CpuUsage;
            _timer.Elapsed += RamUsage;
            _timer.Enabled = true;
        }

        public void OnStart()
        {
            _timer.Start();
        }

        public void OnStop()
        {
            _timer.Stop();
        }

        private int x = 0;
        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>
        
        private void CpuUsage(object sender, EventArgs e)
        {
            var process = Process.GetCurrentProcess();
            _cpuSamples.Add(new DataPoint(x, _cpuCounter.NextValue() / Environment.ProcessorCount));
            var threadCollection = process.Threads.Cast<ProcessThread>();
            foreach (var thread in threadCollection)
            {
                try
                {
                    var point = new DataPoint(x,
                        _cpuCounter.NextValue() / Environment.ProcessorCount * (thread.UserProcessorTime / process.UserProcessorTime));

                    if (threadSeries.ContainsKey(thread.Id))
                    {
                        _threadSamples[threadSeries[thread.Id]].Add(point);
                    }
                    else
                    {
                        threadSeries.Add(thread.Id, _threadSamples.Count);
                        _threadSamples.Add(new List<DataPoint>(){point});
                    }
                }
                catch
                {
                }
            }
            x++;
        }

        private int c = 0;
        private void RamUsage(object sender, EventArgs e)
        {
            _ramSamples.Add(new DataPoint(c,100 * Environment.WorkingSet / (long) _info.TotalPhysicalMemory));
            c++;
        }
        
        private void PerformanceBar(object sender, EventArgs e)
        {
            // todo: avoid boxing
            (LoadModel.Series[0] as BarSeries).Items[0] = new BarItem(CurrentMemoryLoad);
            (LoadModel.Series[0] as BarSeries).Items[1] = new BarItem(CurrentCpuLoad);
        }

        private int cpuLimit = 30;
        
        private void CheckCPULimit(object sender, EventArgs e)
        {
            double v = _cpuCounter.NextValue()/Environment.ProcessorCount;
            if (Math.Abs(v - cpuLimit) > 5)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            }
        }

    }
}