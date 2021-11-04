using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    public partial class ResourceControl : UserControl
    {
        private PerformanceCounter _cpuCounter;
        private DispatcherTimer _dTimer;
        
        public ResourceControl(PerformanceCounter counter)
        {
            _cpuCounter = counter;
            InitializeComponent();
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 700))};
            rc.RenderTarget = RenderTarget.Screen;
            
            CpuPlotView.Model = new PlotModel
            {
                Title = "CPU",
                Series = {new LineSeries(){Title = "Total CPU", Color = OxyColors.Green, LegendKey = "total CPU"} }
            };
            // CpuPlotView.Model.Legends.Add(new Legend
            // {
            //     LegendBackground = OxyColors.Green,
            //     LegendPlacement = LegendPlacement.Outside,
            //     LegendPosition = LegendPosition.BottomCenter,
            //     LegendFontSize = 12
            // });
            (CpuPlotView.Model as IPlotModel).Render(rc, CpuPlotView.Model.PlotArea);
            
            RamPlotView.Model = new PlotModel() {Title = "Memory", Series = {new LineSeries(){Color = OxyColors.Red}}};
            // RamPlotView.Model.Legends.Add(new Legend
            // {
            //     LegendBackground = OxyColors.Red,
            //     LegendPlacement = LegendPlacement.Outside,
            //     LegendPosition = LegendPosition.BottomCenter,
            //     LegendFontSize = 12
            // });
            (RamPlotView.Model as IPlotModel).Render(rc, RamPlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dTimer.Interval = TimeSpan.FromMilliseconds(250);
            _dTimer.Tick += ResourceUsagePlot;
            _dTimer.Start();
        }
        
        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>
        private int x = 0;
        
        private void ResourceUsagePlot(object sender, EventArgs e)
        {
            var process = Process.GetCurrentProcess();
            (CpuPlotView.Model.Series[0] as LineSeries).Points.Add(new DataPoint(x, _cpuCounter.NextValue() / Environment.ProcessorCount));
            (RamPlotView.Model.Series[0] as LineSeries).Points.Add(new DataPoint(x, process.WorkingSet64/1048576));

            ProcessThreadCollection threadCollection = process.Threads;
            foreach (ProcessThread thread in threadCollection)
            {
                try
                {
                    var point = new DataPoint(x,
                        _cpuCounter.NextValue() / Environment.ProcessorCount * (thread.UserProcessorTime / process.UserProcessorTime));

                    if (threadSeries.ContainsKey(thread.Id))
                    {
                        (CpuPlotView.Model.Series[threadSeries[thread.Id]] as LineSeries).Points.Add(point);
                    }
                    else
                    {
                        int i = CpuPlotView.Model.Series.Count;
                        threadSeries.Add(thread.Id, i);
                        var s = new LineSeries(){Color = OxyColors.Brown};
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