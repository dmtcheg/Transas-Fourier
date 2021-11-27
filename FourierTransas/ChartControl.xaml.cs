using OxyPlot;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System.Windows.Threading;
using Services;

namespace FourierTransas
{
    /// <summary>
    /// эмулирует построение и обновление графика сигнала
    /// </summary>
    public partial class ChartControl : UserControl
    {
        private PlotView[] plots;
        private DispatcherTimer _dTimer;
        private Process _process = Process.GetCurrentProcess();
        private CpuCounterService _counterService;

        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();
        
        public ChartControl()
        {
            Thread.BeginThreadAffinity();
            InitializeComponent();
            // использовать realtime?
            _process.PriorityClass = ProcessPriorityClass.RealTime;
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 800))};
            rc.RenderTarget = RenderTarget.Screen;

            plots = new PlotView[]
            {
                PlotView0,
                PlotView1,
                PlotView2
            };
            
            Services.CpuCounterService counterService = new CpuCounterService(); 
            InitializeService(counterService, ThreadPriority.Normal);
            _counterService = counterService;
            Services.CalculationService service = new Services.CalculationService(_counterService);
            InitializeService(service);

            PerfControl.Content = new PerformanceControl(service, counterService);

            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].Model = service?.PlotModels[i];
            }
            
            _dTimer = new DispatcherTimer(DispatcherPriority.Send);
            _dTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dTimer.Tick += SignalPlot;
            _dTimer.Tick += CheckCPULimit;
            _dTimer.IsEnabled = true;
        }

        private void InitializeService(IService service, ThreadPriority priority = ThreadPriority.AboveNormal)
        {
            var thread = new Thread(service.OnStart);
            thread.Priority = priority;
            thread.IsBackground = true;
            thread.Start();
        }

        public static double GetCounterValue() => CounterValue;
        public static double CounterValue { get; private set; }
// как считать использование процессора? часть времени потоки простаивают
        private void SignalPlot(object sender, EventArgs e)
        {
            Thread.BeginThreadAffinity();
            
            var processThread = _process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.UserProcessorTime;
            var p1 = _process.UserProcessorTime;
            
            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].InvalidatePlot(true);
            }

            CounterValue = _counterService.Value/Environment.ProcessorCount * (processThread.UserProcessorTime - t1) / (_process.UserProcessorTime - p1);
        }
        public static double CpuLimit { get; set; } = 10;
        
        //todo: fix limitation
        private void CheckCPULimit(object sender, EventArgs e)
        {
            Func<double, double> f = d =>
            {
                _dTimer.Interval = TimeSpan.FromMilliseconds(d);
                return CounterValue - CpuLimit;
            };
            try
            {
               MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 200, 1000, 3, 5);
            }
            catch
            {
            }
        }
    }
}