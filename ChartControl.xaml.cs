using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
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
                new(3000, 64),
                new(60, 120),
                new(44100, 20)
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
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (obj, ev) =>
            {
                Parallel.For(0, length, i => { points[0][i] = new DataPoint(points[0][i].X, points[0][i].Y + 5); });
                Parallel.For(0, length, i => { points[1][i] = new DataPoint(points[1][i].X, points[1][i].Y + 4); });
                Parallel.For(0, length, i => { points[2][i] = new DataPoint(points[2][i].X, points[2][i].Y + 3); });

                Chart0.InvalidatePlot(true);
                Chart1.InvalidatePlot(true);
                Chart2.InvalidatePlot(true);
            };
            timer.Enabled = flag;
        }
    }
}