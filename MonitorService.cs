using System;
using System.Collections.Generic;
using System.Diagnostics;
using NickStrupat;
using OxyPlot;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System.Timers;

namespace FourierTransas
{
    /// <summary>
    /// сервис для мониторинга и ограничения потребления ресурсов
    /// </summary>
    public class MonitorService : IDisposable
    {
        private Timer _timer;
        PerformanceCounter _cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private ComputerInfo _info = new ComputerInfo();
        private List<DataPoint> _cpuSamples;
        private List<DataPoint> _ramSamples;
        public PlotModel ThreadModel { get; private set; }
        public PlotModel RamModel { get; private set; }

        //todo: fix over 100% "load"
        public double CurrentCpuLoad => _cpuSamples[_cpuSamples.Count - 1].Y;
        public double CurrentMemoryLoad => _ramSamples[_ramSamples.Count - 1].Y;

        public MonitorService()
        {
            _cpuSamples = new List<DataPoint>();
            _ramSamples = new List<DataPoint>();
            ThreadModel = new PlotModel
            {
                Title = "CPU",
                IsLegendVisible = true,
                Series = { new LineSeries() { Title = "Total CPU", Color = OxyColors.Green, Decimator = Decimator.Decimate}}
            };
            ThreadModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            _cpuSamples = (ThreadModel.Series[0] as LineSeries).Points;
            
            RamModel = new PlotModel()
            {
                Title = "Memory",
                IsLegendVisible = true,
                Series = {new LineSeries() {Title="% RAM", Color = OxyColors.Red, Decimator = Decimator.Decimate}}
            };
            RamModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            _ramSamples= (RamModel.Series[0] as LineSeries).Points;
            
            _timer = new Timer(200);
            _timer.Elapsed += (obj, args) => CpuUsage();
            _timer.Elapsed += (obj, args) => RamUsage();
            _timer.Elapsed += (obj, args) => CheckCPULimit();
        }

        public void OnStart()
        {
            _timer.Enabled = true;
        }

        public void OnStop()
        {
            _timer.Enabled = false;
        }

        public void Dispose()
        {
            _timer.Enabled = false;
        }

        private int x = 0;
        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>
        
        private void CpuUsage()
        {
            var counterValue = _cpuCounter.NextValue();
            _cpuSamples.Add(new DataPoint(x,  counterValue/ Environment.ProcessorCount));
            var process = Process.GetCurrentProcess();
            var threadCollection = process.Threads.Cast<ProcessThread>();
            foreach (var thread in threadCollection)
            {
                try
                {
                    var point = new DataPoint(x,
                        counterValue / Environment.ProcessorCount * (thread.UserProcessorTime / process.UserProcessorTime));
            
                    if (threadSeries.ContainsKey(thread.Id))
                    {
                        lock (ThreadModel.SyncRoot)
                        {
                            (ThreadModel.Series[threadSeries[thread.Id]] as LineSeries).Points.Add(point);
                        }
                    }
                    else
                    {
                        threadSeries.Add(thread.Id, ThreadModel.Series.Count);
                        var s = new LineSeries() {Color = OxyColors.Brown, Decimator = Decimator.Decimate};
                        s.Points.Add(point);
                        lock (ThreadModel.SyncRoot)
                        {
                            ThreadModel.Series.Add(s);
                        }
                    }
                }
                catch
                {
                }
            }
            x++;
        }

        private int c = 0;
        private void RamUsage()
        {
            lock (RamModel.SyncRoot)
            {
                _ramSamples.Add(new DataPoint(c, 100 * Environment.WorkingSet / (long) _info.TotalPhysicalMemory));
            }
            c++;
        }

        private readonly int cpuLimit = 30;
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _timer.Interval = d;
                return _cpuCounter.NextValue() / Environment.ProcessorCount - cpuLimit;
            };
            _timer.Interval = MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 50, 600, 3, 5);
        }
    }
}