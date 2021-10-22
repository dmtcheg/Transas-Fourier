using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
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

namespace FourierTransas
{
    public partial class ChartControl : UserControl
    {
        public ChartControl()
        {
            InitializeComponent();
            var view = new OxyPlot.SkiaSharp.Wpf.PlotView();
            var model = new FFTModel(3000, 64);
            view.DataContext = model;
            view.Model = model.Plot;
            Control0.Content = view;

            Chart1.DataContext = new FFTModel(60, 120);
            Chart1.Model = (Chart1.DataContext as FFTModel).Plot;
            Chart2.DataContext = new FFTModel(44100, 20);
            Chart2.Model = (Chart2.DataContext as FFTModel).Plot;
        }

        private bool update = false;

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            update = !update;
            var view = (Control0.Content as OxyPlot.SkiaSharp.Wpf.PlotView);
            var model = view.Model;

            var rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 600))};
            rc.RenderTarget = RenderTarget.Screen;

            var pts = (model.Series[0] as LineSeries).Points;
            var len = pts.Count;

            var updated = new DataPoint[len];
            Parallel.For(0, len, (x) => { updated[x] = new DataPoint(x, pts[x].Y + 10); });
            view.Model.Series[0] = new LineSeries() {ItemsSource = updated};
            view.InvalidatePlot(true);
        }

        // private void SkiaRender(PlotModel model)
        // {
        //     var rc = new SkiaRenderContext();
        //     rc.RenderTarget = RenderTarget.Screen;
        //     (model as IPlotModel).Render(rc, model.PlotArea);
        // }
    }
}