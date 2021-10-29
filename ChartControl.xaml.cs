using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System.Timers;

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

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            var rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 700))};
            rc.RenderTarget = RenderTarget.PixelGraphic;
            flag = !flag;
            
            var charts = new PlotView[] {Chart0, Chart1, Chart2};
            var points = new List<DataPoint>[3];
            for (var i = 0; i < 3; i++) points[i] = (charts[i].Model.Series[0] as LineSeries).Points;
            var length = points[0].Count;
            var step = 50;
            
            // optional: if (2n click) then timer stop
            var timer = new System.Timers.Timer(100);
            timer.Elapsed += (obj, ev) =>
            {
                // var w = new Stopwatch();
                // w.Start();
                Parallel.For(0, 3, i=>
                {
                    var first = points[i][0];
                    for (int j = 0; j < length-step; j+=step)
                    {
                        points[i][j] = new DataPoint(points[i][j].X, points[i][j+step].Y);
                    }
                    points[i][length - step] = new DataPoint(points[i][length - step].X, first.Y);
                    charts[i].InvalidatePlot(true);
                });
                // w.Stop();
                // Console.WriteLine(w.ElapsedTicks);
            };
            timer.Enabled = true;
        }
    }
}