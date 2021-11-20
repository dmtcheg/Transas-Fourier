using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System.Windows.Threading;

namespace FourierTransas
{
    public partial class ChartControl : UserControl
    {
        private PlotView[] plots;
        private DispatcherTimer _dTimer;
        //private CalculationService _service;

        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetCurrentThread();
        
        /// <summary>
        /// эмулирует построение и обновление графика сигнала
        /// </summary>
        public ChartControl()
        {
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

            CalculationService service = null;
            //todo

            // var calcThreadId =IntPtr.Zero;
            // var calcThread = new Thread(()=>
            // {
            //     service = new CalculationService();
            //     service.OnStart();
            // });
            // calcThread.Priority = ThreadPriority.AboveNormal;
            // calcThread.IsBackground = true;
            // calcThread.Start();
            
            service = new CalculationService();
            Task t = Task.Factory.StartNew(() =>
            {
                service.OnStart();
            }, TaskCreationOptions.LongRunning);
            
            PerfControl.Content = new PerformanceControl(GetCurrentThreadId(), service);

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
        private PerformanceCounter _timeCounter = new PerformanceCounter();
        private void SignalPlot(object sender, EventArgs e)
        {
            var process = Process.GetCurrentProcess();
            var processThread = process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.TotalProcessorTime;
            var p1 = process.TotalProcessorTime;
            
            for (int i = 0; i < plots.Length; i++)
            {
                plots[i].InvalidatePlot(true);
            }
            CounterValue = (processThread.UserProcessorTime - t1) / (process.UserProcessorTime - p1);
        }
    }
}