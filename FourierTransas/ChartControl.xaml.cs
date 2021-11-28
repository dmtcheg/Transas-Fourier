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
        private Timer _limitTimer;
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
            
            CpuCounterService counterService = new CpuCounterService(); 
            InitializeService(counterService, ThreadPriority.Normal);
            _counterService = counterService;
            CalculationService service = new CalculationService(_counterService);
            InitializeService(service);

            PerfControl.Content = new PerformanceControl(service, counterService);

            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].Model = service?.PlotModels[i];
            }
            
            _dTimer = new DispatcherTimer(DispatcherPriority.Send);
            _dTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dTimer.Tick += SignalPlot;
            _dTimer.IsEnabled = true;

            _limitTimer = new Timer(new TimerCallback(state => CheckCPULimit()), null, 0, 5000);
            processInitTime = _process.TotalProcessorTime;
            threadTime = TimeSpan.Zero;
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
        private TimeSpan threadTime;
        private TimeSpan processInitTime;
        
        private void SignalPlot(object sender, EventArgs e)
        {
            Thread.BeginThreadAffinity();
            _process.Refresh();
            var processThread = _process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.TotalProcessorTime;
            
            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].InvalidatePlot(true);
            }

            threadTime += processThread.TotalProcessorTime - t1;
            CounterValue = _counterService.Value * threadTime / (_process.TotalProcessorTime - processInitTime);
        }
        public static double CpuLimit { get; set; } = 10;
        private bool isRootFinding = false;

        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _dTimer.Interval = TimeSpan.FromMilliseconds(d);
                Thread.Sleep((int)(d*10));
                return CounterValue - CpuLimit;
            };
            if (isRootFinding)
                return;
            isRootFinding = true;
            double interval;
            if (MathNet.Numerics.RootFinding.Bisection.TryFindRoot(f, 200, 1000, 3, 5, out interval))
                _dTimer.Interval = TimeSpan.FromMilliseconds(interval);
            isRootFinding = false;
        }
    }
}