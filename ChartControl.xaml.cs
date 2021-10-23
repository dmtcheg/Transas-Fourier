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
using  System.Timers;

namespace FourierTransas
{
    public partial class ChartControl : UserControl
    {
        //todo: correct render

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

        private bool flag = false;
        
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            // var rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 600))};
            // rc.RenderTarget = RenderTarget.Screen;
            
            flag = !flag;
            var view = (Control0.Content as OxyPlot.SkiaSharp.Wpf.PlotView);
            var points = (view.Model.Series[0] as LineSeries).Points;
            var length = points.Count;
            
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (obj, ev) =>
            {
                Parallel.For(0, length, i => { points[i] = new DataPoint(points[i].X, points[i].Y +10); });
                view.InvalidatePlot(true);
            };
            timer.Enabled = flag;
        }
    }
}