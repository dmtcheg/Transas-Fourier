using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot.SkiaSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.SkiaSharp;

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
            var view = Control0.Content as OxyPlot.SkiaSharp.Wpf.PlotView;

            var pts = (view.Model.Series[0] as LineSeries).Points;
            var len = pts.Count;
            for (int i = 0; i < len && update; i++)
            {
                pts[i] = new DataPoint(i, pts[i].Y + 10);
                view.InvalidatePlot(true);
                Thread.Sleep(500);
            }
        }

        private void SkiaRender()
        {
            var rc = new SkiaRenderContext();
            rc.RenderTarget = RenderTarget.Screen;
            
        }
    }
}
