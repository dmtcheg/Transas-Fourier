using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System.Timers;
using System.Windows.Threading;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace FourierTransas
{
    public partial class ChartControl : UserControl
    {
        private PlotView[] plots;
        List<DataPoint>[] points;
        private int length;
        PerformanceCounter _counter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private DispatcherTimer _dTimer;
        private Random r = new Random();
        private float cpuLimit = 30;

        public ChartControl()
        {
            InitializeComponent();
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 700))};
            rc.RenderTarget = RenderTarget.Screen;

            FFTModel[] models = new FFTModel[]
            {
                new(2000, 15),
                new(6000, 35),
                new(4400, 65)
            };
            plots = new PlotView[]
            {
                PlotView0,
                PlotView1,
                PlotView2
            };
            for (var i = 0; i < 3; i++)
            {
                plots[i].DataContext = models[i];
                plots[i].Model = models[i].Plot;
                (plots[i].Model as IPlotModel).Render(rc, plots[i].Model.PlotArea);
            }

            points = new List<DataPoint>[]
            {
                (PlotView0.Model.Series[0] as LineSeries).Points,
                (PlotView1.Model.Series[0] as LineSeries).Points,
                (PlotView2.Model.Series[0] as LineSeries).Points,
            };
            length = points[0].Count;

            var usage = new PlotModel() {Title = "cpu usage"};
            usage.Series.Add(new LineSeries());
            CpuPlotView.Model = usage;
            ((IPlotModel) CpuPlotView.Model).Render(rc, CpuPlotView.Model.PlotArea);
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            _dTimer = new DispatcherTimer(DispatcherPriority.Render);
            _dTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dTimer.Tick += SignalPlot;
            _dTimer.Tick += CpuUsagePlot;
            _dTimer.Tick += CheckCPULimit;
            _dTimer.Start();
        }

        private void SignalPlot(object sender, EventArgs e)
        {
            _counter.NextValue();

            double[] gen = Generate.Sinusoidal(length, length * 2, r.Next(0, 199999), r.Next(0, 100));
            Complex[] complex = new Complex[length];
            for (int j = 0; j < length; j++) complex[j] = new Complex(gen[j], 0);

            Fourier.Forward(complex, FourierOptions.NoScaling);
            for (int j = 0; j < length; j++)
                gen[j] = Math.Sqrt(Math.Pow(complex[j].Real, 2) + Math.Pow(complex[j].Imaginary, 2)) * 2 / length;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    points[i][j] = new DataPoint(points[i][j].X, points[i][j].Y + gen[j]);
                }

                plots[i].InvalidatePlot(true);
            }

            _counter.NextValue();
        }

        private int x = 0;
        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>

        private void CpuUsagePlot(object sender, EventArgs e)
        {
            var process = Process.GetCurrentProcess();
            ProcessThreadCollection threadCollection = process.Threads;
            foreach (ProcessThread thread in threadCollection)
            {
                var point = new DataPoint(x,
                    _counter.NextValue()/Environment.ProcessorCount * (thread.UserProcessorTime / process.UserProcessorTime));

                if (threadSeries.ContainsKey(thread.Id))
                {
                    (CpuPlotView.Model.Series[threadSeries[thread.Id]] as LineSeries).Points.Add(point);
                }
                else
                {
                    int i = CpuPlotView.Model.Series.Count;
                    threadSeries.Add(thread.Id, i);
                    var s = new LineSeries();
                    s.Points.Add(point);
                    CpuPlotView.Model.Series.Add(s);
                }
            }

            x++;
            CpuPlotView.InvalidatePlot(true);
        }

        private void CheckCPULimit(object sender, EventArgs e)
        {
            double v = _counter.NextValue()/Environment.ProcessorCount;
            if (Math.Abs(v - cpuLimit) > 7)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            }
        }
    }
}