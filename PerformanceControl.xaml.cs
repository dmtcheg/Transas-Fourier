﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    public partial class PerformanceControl : UserControl
    {
        PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        PerformanceCounter _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
        private DispatcherTimer _dTimer;

        public PerformanceControl()
        {
            InitializeComponent();
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 700))};
            rc.RenderTarget = RenderTarget.Screen;
            
            var resourceModel = new PlotModel();
            var s = new BarSeries();
            s.Items.Add(new BarItem(_ramCounter.NextValue()));
            s.Items.Add(new BarItem(_cpuCounter.NextValue()/Environment.ProcessorCount));
            resourceModel.Series.Add(s);
            resourceModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "ResourceAxis",
                ItemsSource = new[]{"Mem", "CPU"}
            });
            PerformancePlotView.Model = resourceModel;
            (PerformancePlotView.Model as IPlotModel).Render(rc, PerformancePlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dTimer.Tick += PerformanceBar;
            _dTimer.Start();
        }

        private void PerformanceBar(object sender, EventArgs e)
        {
            (PerformancePlotView.Model.Series[0] as BarSeries).Items[0] = new BarItem(_ramCounter.NextValue());
            (PerformancePlotView.Model.Series[0] as BarSeries).Items[1] = new BarItem(_cpuCounter.NextValue()/Environment.ProcessorCount);
            PerformancePlotView.InvalidatePlot(true);
        }

        private void PerformancePlotView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Window resourceWindow = new Window
            {
                Title = "Использование ресурсов",
                Content = new ResourceControl()
            };
            resourceWindow.ShowDialog();
        }
    }
}