using System;
using System.Collections.Generic;
using System.Diagnostics;
using NickStrupat;
using OxyPlot;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using OxyPlot.Legends;
using OxyPlot.Series;
using Timer = System.Timers.Timer;

namespace FourierTransas
{
    /// <summary>
    /// сервис для мониторинга и ограничения потребления ресурсов
    /// </summary>
    public class MonitorService : IDisposable
    {
        public CalculationService CalculationService { get; private set; }
        private Timer _timer;

        static PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);

        static ComputerInfo _info = new ComputerInfo();
        Process _process = Process.GetCurrentProcess();
        private List<DataPoint> _cpuSamples;
        private List<DataPoint> _ramSamples;
        public PlotModel ThreadModel { get; private set; }
        public PlotModel RamModel { get; private set; }
        public double CounterValue { get; private set; }

        public MonitorService(CalculationService service)
        {
            CalculationService = service;
            ThreadModel = new PlotModel
            {
                Title = "CPU",
                IsLegendVisible = true,
                Series = {new LineSeries() {Title = "Total CPU", Color = OxyColors.Green, Decimator = Decimator.Decimate}}
            };
            ThreadModel.Series.Add(new LineSeries()
                {Color = OxyColors.Orange, Title = "plot render", Decimator = Decimator.Decimate});
            ThreadModel.Series.Add(new LineSeries()
                {Color = OxyColors.Blue, Title = "recource monitor", Decimator = Decimator.Decimate});
            ThreadModel.Series.Add(new LineSeries()
                {Color = OxyColors.Brown, Title = "calculation", Decimator = Decimator.Decimate});

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
                Series = {new LineSeries() {Title = "% RAM", Color = OxyColors.Red, Decimator = Decimator.Decimate}}
            };
            RamModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            _ramSamples = (RamModel.Series[0] as LineSeries).Points;
            _timer = new Timer(1000);
            _timer.Elapsed += CpuRamUsage;
            _timer.Elapsed += CheckCPULimit;
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

        public void Dispose()
        {
            _timer.Enabled = false;
        }
        
        [DllImport("Kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        public static double CurrentMemoryLoad()
        {
            return 100 * Environment.WorkingSet / (long) _info.TotalPhysicalMemory;
        }

        public static double CurrentCpuLoad()
        {
            return _cpuCounter.NextValue() / Environment.ProcessorCount;
        }

        private void CpuRamUsage(object sender, ElapsedEventArgs e)
        {
            _cpuCounter.NextValue();

            var processThread = _process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.TotalProcessorTime;
            var p1 = _process.TotalProcessorTime;
            int x = _ramSamples.Count + 1;
            _ramSamples.Add(new DataPoint(x, 100 * Environment.WorkingSet / (long) _info.TotalPhysicalMemory));
            lock (ThreadModel.SyncRoot)
            {
                (ThreadModel.Series[1] as LineSeries).Points.Add(new DataPoint(x,
                    100 * (ChartControl.CounterValue)));
                (ThreadModel.Series[3] as LineSeries).Points.Add(new DataPoint(x,
                    100 * CalculationService.CounterValue));
            }

            CounterValue = (processThread.UserProcessorTime - t1) / (_process.UserProcessorTime - p1) /
                           Environment.ProcessorCount;
            lock (ThreadModel.SyncRoot)
            {
                (ThreadModel.Series[2] as LineSeries).Points.Add(new DataPoint(x, 100 * CounterValue));
            }
            _cpuSamples.Add(new DataPoint(x, _cpuCounter.NextValue() / Environment.ProcessorCount));

        }

        private readonly int cpuLimit = 5;
        private void CheckCPULimit(object sender, EventArgs e)
        {
            Func<double, double> f = d =>
            {
                _timer.Interval = d;
                return CounterValue - cpuLimit;
            };
            _timer.Interval = MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 200, 1000, 3, 3);
        }
    }
}