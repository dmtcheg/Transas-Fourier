using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System.Timers;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace FourierTransas
{
    public partial class ChartControl : UserControl
    {
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
                Chart0,
                Chart1,
                Chart2
            };
            for (var i = 0; i < 3; i++)
            {
                plots[i].DataContext = models[i];
                plots[i].Model = models[i].Plot;
                (plots[i].Model as IPlotModel).Render(rc, plots[i].Model.PlotArea);
            }
            points = new List<DataPoint>[]
            {
                (Chart0.Model.Series[0] as LineSeries).Points,
                (Chart1.Model.Series[0] as LineSeries).Points,
                (Chart2.Model.Series[0] as LineSeries).Points,
            };
        }

        private bool flag = false;
        private Timer timer = new Timer(100);
        private PlotView[] plots;
        List<DataPoint>[] points;
        private PerformanceCounter _counter;
        
        //debug
        // sync 300-400 ms 315 в конце
        // parallel 360-400

        //release
        // sync 50-67ms
        // parallel 80-90ms

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            flag = !flag;
            _counter = new PerformanceCounter("Process", "% Processor Time", "_Total");
            timer.Enabled = flag;
            if (flag)
            {
                timer.Elapsed += UpdatePlot;
                timer.Elapsed += CheckCPUUsage;
            }
        }

        private void UpdatePlot(object sender, ElapsedEventArgs e)
        {
            Random r = new Random();

            int length = points[0].Count;
            double[] gen = Generate.Sinusoidal(length, length * 2, r.Next(0, 199999), r.Next(0, 100));
            Complex[] complex = new Complex[length];
            for (int j = 0; j < length; j++)
            {
                complex[j] = new Complex(gen[j], 0);
            }

            Fourier.Forward(complex, FourierOptions.NoScaling);
            for (int j = 0; j < length; j++)
            {
                gen[j] = Math.Sqrt(Math.Pow(complex[j].Real, 2) + Math.Pow(complex[j].Imaginary, 2)) * 2 / length;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    points[i][j] = new DataPoint(points[i][j].X, points[i][j].Y + gen[j]);
                }

                plots[i].InvalidatePlot(true);
            }
        }

        private void CheckCPUUsage(object sender, ElapsedEventArgs e)
        {
            // todo: fix to app cpu usage
            double limit = 300.0; // measure?
            if (_counter.NextValue() > limit)
            {
                timer.Interval *= (limit / _counter.NextValue());
            }
        }
    }
}