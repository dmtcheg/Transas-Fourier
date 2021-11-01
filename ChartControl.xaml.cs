using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
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
            var rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 700))};
            rc.RenderTarget = RenderTarget.PixelGraphic;

            var models = new FFTModel[]
            {
                new(2000, 15),
                new(6000, 35),
                new(4400, 65)
            };
            var charts = new PlotView[]
            {
                Chart0,
                Chart1,
                Chart2
            };
            for (var i = 0; i < 3; i++)
            {
                charts[i].DataContext = models[i];
                charts[i].Model = models[i].Plot;
                (charts[i].Model as IPlotModel).Render(rc, charts[i].Model.PlotArea);
            }
        }

        private bool flag = false;

        //debug
        // sync 300-400 ms 315 в конце
        // parallel 360-400
        
        //release
        // sync 50-67ms
        // parallel 80-90ms
        
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            var rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 700))};
            rc.RenderTarget = RenderTarget.PixelGraphic;
            flag = !flag;

            var charts = new PlotView[] {Chart0, Chart1, Chart2};
            var points = new List<DataPoint>[3];
            for (var i = 0; i < 3; i++) points[i] = (charts[i].Model.Series[0] as LineSeries).Points;
            var length = points[0].Count;
            var step = 1;
            var r = new Random();
            double sum = 0;
            int k = 0;

            // optional: if (2n click) then timer stop
            var timer = new System.Timers.Timer(50);
            timer.Elapsed += (obj, ev) =>
            {
                double[] gen = Generate.Sinusoidal(length, length * 2, r.Next(0, 199999), r.Next(0, 100));
                var complex = new Complex[length];
                for (int j = 0; j < length; j++)
                {
                    complex[j] = new Complex(gen[j], 0);
                }

                Fourier.Forward(complex, FourierOptions.NoScaling);
                for (int j = 0; j < length; j++)
                {
                    gen[j] = Math.Sqrt(Math.Pow(complex[j].Real, 2) + Math.Pow(complex[j].Imaginary, 2)) * 2 / length;
                }

                var w = new Stopwatch();
                w.Start();
                for(int i= 0; i<3; i++)
                {
                    for (int j = 0; j < length; j += step)
                    {
                        points[i][j] = new DataPoint(points[i][j].X, points[i][j].Y + gen[j]);
                    }
                    charts[i].InvalidatePlot(true);
                }
                w.Stop();
                sum += w.ElapsedMilliseconds;
                k++;
                Console.WriteLine(sum/k);
            };
            timer.Enabled = true;
        }
    }
}