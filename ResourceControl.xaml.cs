using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    public partial class ResourceControl : UserControl
    {
        private PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private PerformanceCounter _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
        private DispatcherTimer _dTimer;
        
        public ResourceControl()
        {
            InitializeComponent();
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(1000, 700))};
            rc.RenderTarget = RenderTarget.Screen;
            
            CpuPlotView.Model = new PlotModel(){Title = "CPU", Series = { new LineSeries()}};
            (CpuPlotView.Model as IPlotModel).Render(rc, CpuPlotView.Model.PlotArea);
            
            RamPlotView.Model = new PlotModel() {Title = "Memory", Series = {new LineSeries()}};
            (RamPlotView.Model as IPlotModel).Render(rc, RamPlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dTimer.Tick += ResourceUsagePlot;
            _dTimer.Start();
        }
        
        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>
        private int x = 0;
        
        private void ResourceUsagePlot(object sender, EventArgs e)
        {
            (CpuPlotView.Model.Series[0] as LineSeries).Points.Add(new DataPoint(x, _cpuCounter.NextValue() / Environment.ProcessorCount));
            (RamPlotView.Model.Series[0] as LineSeries).Points.Add(new DataPoint(x, _ramCounter.NextValue()));

            var process = Process.GetCurrentProcess();
            ProcessThreadCollection threadCollection = process.Threads;
            foreach (ProcessThread thread in threadCollection)
            {
                try
                {
                    var time = thread.UserProcessorTime;
                    var point = new DataPoint(x,
                        _cpuCounter.NextValue() / Environment.ProcessorCount * (time / process.UserProcessorTime));

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
                catch
                {
                }
            }
            x++;
            CpuPlotView.InvalidatePlot(true);
            RamPlotView.InvalidatePlot(true);
        }
    }
}