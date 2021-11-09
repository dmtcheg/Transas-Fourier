using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
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
        private float cpuLimit = 32;
        
        public ChartControl()
        {
            InitializeComponent();
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 800))};
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
            _dTimer = new DispatcherTimer(DispatcherPriority.Send);
            _dTimer.Interval = TimeSpan.FromMilliseconds(80);
            _dTimer.Tick += SignalPlot;
            //_dTimer.Tick += CheckCPULimit;
            _dTimer.Start();
        }
        
        private void CheckCPULimit(object sender, EventArgs e)
        {
            double v = _counter.NextValue()/Environment.ProcessorCount;
            if (Math.Abs(v - cpuLimit) > 5)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            }
        }

        private void SignalPlot(object sender, EventArgs e)
        {
            _counter.NextValue();

            double[] gen = Generate.Sinusoidal(length, length * 2, r.Next(0, 199999), r.Next(0, 100));
            Complex[] complex = new Complex[length];
            for (int j = 0; j < length; j++) complex[j] = new Complex(gen[j], 0);

            Fourier.Forward(complex, FourierOptions.NoScaling);
            for(int j=0;j<length; j++)
                gen[j] = Math.Sqrt(Math.Pow(complex[j].Real, 2) + Math.Pow(complex[j].Imaginary, 2)) * 2 / length;

            Parallel.For(0,3,i=>
            {
                for (int j = 0; j < length; j++)
                {
                    points[i][j] = new DataPoint(points[i][j].X, points[i][j].Y + gen[j]*Math.Pow(-1, j+i));
                }

                plots[i].InvalidatePlot(true);
            });

            _counter.NextValue();
        }
    }
}