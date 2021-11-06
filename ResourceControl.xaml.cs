using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 400))};
            rc.RenderTarget = RenderTarget.Screen;

            PlotModel cpuModel = new PlotModel
            {
                Title = "CPU",
                IsLegendVisible = true,
                Series = { new LineSeries() { Title = "Total CPU", Color = OxyColors.Green, Decimator = Decimator.Decimate}}
            };
            cpuModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            CpuPlotView.Model = cpuModel;
            (CpuPlotView.Model as IPlotModel).Render(rc, CpuPlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Send);
            _dTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dTimer.Tick += ResourceUsagePlot;
            _dTimer.Start();
        }

        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>
        private int x = 0;

        private void ResourceUsagePlot(object sender, EventArgs e)
        {
            var process = Process.GetCurrentProcess();
            (CpuPlotView.Model.Series[0] as LineSeries).Points.Add(new DataPoint(x,
                _cpuCounter.NextValue() / Environment.ProcessorCount));

            var threadCollection = process.Threads.Cast<ProcessThread>();
            foreach (var thread in threadCollection)
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
                        threadSeries.Add(thread.Id, CpuPlotView.Model.Series.Count);
                        var s = new LineSeries() {Color = OxyColors.Brown};
                        s.Points.Add(point);
                        CpuPlotView.Model.Series.Add(s);
                    }
                }
                catch
                {
                }
            }
            x++;
            //optional: use dispatcher.beginInvoke
            CpuPlotView.InvalidatePlot(true);
        }
    }
}