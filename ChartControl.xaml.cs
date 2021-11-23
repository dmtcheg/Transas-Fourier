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

namespace FourierTransas
{
    /// <summary>
    /// эмулирует построение и обновление графика сигнала
    /// </summary>
    public partial class ChartControl : UserControl
    {
        private PlotView[] plots;
        private DispatcherTimer _dTimer;

        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();
        
        public ChartControl()
        {
            Thread.BeginThreadAffinity();
            InitializeComponent();
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 800))};
            rc.RenderTarget = RenderTarget.Screen;

            plots = new PlotView[]
            {
                PlotView0,
                PlotView1,
                PlotView2
            };

            CalculationService service = new CalculationService();
            var calcThread = new Thread(()=>
            {
                service.OnStart();
            });
            calcThread.Priority = ThreadPriority.AboveNormal;
            calcThread.IsBackground = true;
            calcThread.Start();
            calcThread.Join();
            // Task t = Task.Factory.StartNew(() =>
            // {
            //     service.OnStart();
            // }, TaskCreationOptions.LongRunning);
            
            PerfControl.Content = new PerformanceControl(service);

            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].Model = service?.PlotModels[i];
            }
            
            _dTimer = new DispatcherTimer(DispatcherPriority.Send);
            _dTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dTimer.Tick += SignalPlot;
            _dTimer.IsEnabled = true;
        }
        
        public static double CounterValue { get; private set; }
        private Process _process = Process.GetCurrentProcess();
        public static double CpuLimit { get; set; } = 10;

        private void SignalPlot(object sender, EventArgs e)
        {
            var processThread = _process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.UserProcessorTime;
            var p1 = _process.UserProcessorTime;
            
            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].InvalidatePlot(true);
            }
            CounterValue = (processThread.UserProcessorTime - t1) / (_process.UserProcessorTime - p1)/Environment.ProcessorCount;
        }
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _dTimer.Interval = TimeSpan.FromMilliseconds(d);
                return CounterValue - CpuLimit;
            };
            _dTimer.Interval = TimeSpan.FromMilliseconds(MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 100, 600, 3, 4));
        }
    }
}