using System;
using System.Collections.Generic;
using System.Diagnostics;
using NickStrupat;
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
        static PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        static ComputerInfo _info = new ComputerInfo();
        Process _process = Process.GetCurrentProcess();

        public PlotModel ThreadModel { get; private set; }
        private List<DataPoint> _cpuSamples;
        private List<List<DataPoint>> _threadSamples;
        
        public PlotModel RamModel { get; private set; }
        private List<DataPoint> _ramSamples;
        
        public PlotModel BarModel { get; private set; }
        private List<BarItem> _items;

        public double CounterValue { get; private set; }
        private Func<double> _mainCounterValue;

        public MonitorService(CalculationService service, Func<double> mainCounterValue, IService counter)
        {
            CalculationService = service;
            _counterService = counter as CpuCounterService;;
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
            _cpuSamples = (ThreadModel.Series[0] as LineSeries).Points;
            _threadSamples = new List<List<DataPoint>>(ThreadModel.Series.Count - 1);
            for (int i = 1; i < ThreadModel.Series.Count; i++)
            {
                _threadSamples.Add((ThreadModel.Series[i] as LineSeries).Points);
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
            _ramSamples = (RamModel.Series[0] as LineSeries).Points;

            // BarModel = new PlotModel()
            // {
            //     Series = {new BarSeries() {Items = {new BarItem(0), new BarItem(0)}}},
            //     Axes =
            //     {
            //         new CategoryAxis()
            //         {
            //             Position = AxisPosition.Left,
            //             Key = "ResourceAxis",
            //             ItemsSource = new[] {"Mem", "CPU"}
            //         }
            //     }
            // };
            // _items = (BarModel.Series[0] as BarSeries).Items;

            // todo: один таймер у всех сервисов?
            
        }

        public void OnStart()
        {
            Thread.BeginThreadAffinity();
            var callback = new TimerCallback((state) =>
            {
                CpuRamUsage();
                //CheckCPULimit();
            });
            // часто?
            _period = 1000;
            _timer = new Timer(callback, null, 0,_period);
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
            var ramSeries = RamModel.Series;
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

        // todo: https://github.com/openhardwaremonitor/openhardwaremonitor/tree/master/Collections
        private void CpuRamUsage()
        {;
            Thread.BeginThreadAffinity();
            
            var pThreads = Process.GetCurrentProcess().Threads.Cast<ProcessThread>().ToArray();
            var processThread = pThreads.First(p => p.Id == GetCurrentThreadId());
            
            var t1 = processThread.TotalProcessorTime;
            var p1 = _process.TotalProcessorTime;
            
            int x = _ramSamples.Count + 1;
            _ramSamples.Add(new DataPoint(x, _counterService.RamValue));

            //todo: msbuild. for .net framework?
            lock (ThreadModel.SyncRoot)
            {
                _threadSamples[0].Add(new DataPoint(x,_mainCounterValue()));
                _threadSamples[2].Add(new DataPoint(x, CalculationService.CounterValue));
            }
            
            CounterValue = _counterService.Value * (processThread.UserProcessorTime-t1) / (_process.UserProcessorTime-p1);
            lock (ThreadModel.SyncRoot)
            {
                _threadSamples[1].Add(new DataPoint(x, CounterValue));
            }
            _cpuSamples.Add(new DataPoint(x, _counterService.Value));
        }

        public double CpuLimit { get; set; } = 4;
        //todo: maybe add "mean" counterValue
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _period = (int)d;
                _timer.Change(0, _period);
                return CounterValue - CpuLimit;
            };
            try
            {
                _period = (int)MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 500, 1000, 2, 4);
                _timer.Change(0, _period);
            }
            catch
            {
            }
        }
    }
}