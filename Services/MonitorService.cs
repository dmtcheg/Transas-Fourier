using System;
using System.Collections.Generic;
using System.Diagnostics;
using OxyPlot;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace Services
{
    /// <summary>
    /// сервис для мониторинга и ограничения потребления ресурсов
    /// </summary>
    public class MonitorService : IService
    {
        public CalculationService CalculationService { get; private set; }
        private CpuCounterService _counterService;
        
        private Timer _timer;
        private int _period;
        Process _process = Process.GetCurrentProcess();

        public PlotModel ThreadModel { get; private set; }
        public PlotModel RamModel { get; private set; }

        #region buffers
        private CircularBuffer<DataPoint> _ramSamples;
        private int capacity = 32;
        private CircularBuffer<DataPoint> _cpuSamples;
        private List<CircularBuffer<DataPoint>> _threadSamples;
        #endregion
        
        public double CounterValue { get; private set; }
        private Func<double> _mainCounterValue;
        
        public MonitorService(CalculationService service, Func<double> mainCounterValue, CpuCounterService counter)
        {
            CalculationService = service;
            _counterService = counter;
            _mainCounterValue = mainCounterValue;
            
            ThreadModel = new PlotModel
            {
                Title = "CPU",
                IsLegendVisible = true,
                Series =
                {
                    new LineSeries() { Title = "Total CPU", Color = OxyColors.Green, Decimator = Decimator.Decimate }
                }
            };
            ThreadModel.Series.Add(new LineSeries()
                { Color = OxyColors.Orange, Title = "plot render", Decimator = Decimator.Decimate });
            ThreadModel.Series.Add(new LineSeries()
                { Color = OxyColors.Blue, Title = "recource monitor", Decimator = Decimator.Decimate });
            ThreadModel.Series.Add(new LineSeries()
                { Color = OxyColors.Brown, Title = "calculation", Decimator = Decimator.Decimate });

            ThreadModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            
            _cpuSamples = new CircularBuffer<DataPoint>(capacity);
            (ThreadModel.Series[0] as LineSeries).ItemsSource = _cpuSamples;
            _threadSamples = new List<CircularBuffer<DataPoint>>(3);
            for (int i = 1; i < ThreadModel.Series.Count; i++)
            {
                _threadSamples.Add(new CircularBuffer<DataPoint>(capacity));
                (ThreadModel.Series[i] as LineSeries).ItemsSource = _threadSamples[i-1];
            }

            RamModel = new PlotModel()
            {
                Title = "Memory",
                IsLegendVisible = true,
                Series = { new LineSeries() { Title = "% RAM", Color = OxyColors.Red, Decimator = Decimator.Decimate } }
            };
            RamModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            _ramSamples = new CircularBuffer<DataPoint>(capacity);
            (RamModel.Series[0] as LineSeries).ItemsSource = _ramSamples;
        }

        public void OnStart()
        {
            Thread.BeginThreadAffinity();
            var callback = new TimerCallback((state) =>
            {
                CpuRamUsage();
            });
            _period = 1000;
            _timer = new Timer(callback, null, 0, _period);
            processInitTime = _process.TotalProcessorTime;
        }

        public void OnStop()
        {
            _timer.Dispose();
            CreatePlotModel();
        }

        /// <summary>
        /// creates not attached PlotModel
        /// </summary>
        private void CreatePlotModel()
        {
            var series = ThreadModel.Series;
            var legends = ThreadModel.Legends;
            var ramLegend = RamModel.Legends;

            ThreadModel = new PlotModel();
            ThreadModel.Series.Add(new LineSeries()
            {
                ItemsSource = _cpuSamples,
                Title = series[0].Title,
                Decimator = Decimator.Decimate,
                Color = OxyColors.Green
            });
            for (int i = 1; i < series.Count; i++)
            {
                ThreadModel.Series.Add(new LineSeries()
                {
                    ItemsSource = _threadSamples[i - 1],
                    Decimator = Decimator.Decimate,
                    Color = (series[i] as LineSeries).Color,
                    Title = series[i].Title
                });
            }
            foreach (var l in legends)
            {
                ThreadModel.Legends.Add(new Legend()
                {
                    LegendPlacement = l.LegendPlacement,
                    FontSize = l.FontSize,
                    LegendPosition = l.LegendPosition
                });
            }

            RamModel = new PlotModel();
            RamModel.Series.Add(new LineSeries()
            {
                ItemsSource = _ramSamples,
                Title = "% RAM",
                Color = OxyColors.Red,
                Decimator = Decimator.Decimate
            });
            foreach (var l in ramLegend)
            {
                RamModel.Legends.Add(new Legend()
                {
                    LegendPlacement = l.LegendPlacement,
                    FontSize = l.FontSize,
                    LegendPosition = l.LegendPosition
                });
            }
        }

        [DllImport("Kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private TimeSpan threadTime;
        private TimeSpan processInitTime;
        private int x = 0;
        private void CpuRamUsage()
        {;
            Thread.BeginThreadAffinity();
            _process.Refresh();
            var pThreads = _process.Threads.Cast<ProcessThread>().ToArray();
            var processThread = pThreads.First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.TotalProcessorTime;
            
            _ramSamples.PushBack(new DataPoint(x, _counterService.RamValue));

            lock (ThreadModel.SyncRoot)
            {
                _threadSamples[0].PushBack(new DataPoint(x,_mainCounterValue()));
                _threadSamples[2].PushBack(new DataPoint(x, CalculationService.CounterValue));
            }

            threadTime += (processThread.TotalProcessorTime - t1);
            CounterValue = _counterService.Value * threadTime/ (_process.TotalProcessorTime-processInitTime);
            lock (ThreadModel.SyncRoot)
            {
                _threadSamples[1].PushBack(new DataPoint(x, CounterValue));
            }
            
            _cpuSamples.PushBack(new DataPoint(x, _counterService.Value));
            x++;
        }

        public double CpuLimit { get; set; } = 4;
        private bool isRootFinding = false;
        
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _period = (int)d;
                _timer.Change(_period, 5*_period);
                return CounterValue - CpuLimit;
            };
            double interval;
            if (isRootFinding)
                return;
            isRootFinding = true;
            if (MathNet.Numerics.RootFinding.Bisection.TryFindRoot(f, 50, 800, 2, 4, out interval))
                _timer.Change(0, (int)interval);
            isRootFinding = false;
        }
    }
}