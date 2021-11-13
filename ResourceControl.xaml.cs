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
        private MonitorService _service;
        private DispatcherTimer _dTimer;

        public ResourceControl(MonitorService service)
        {
            InitializeComponent();
            _service = service;
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

            _dTimer = new DispatcherTimer(DispatcherPriority.Render);
            _dTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dTimer.Tick += ResourceUsagePlot;
            _dTimer.Start();
        }

        Dictionary<int, int> threadSeries = new Dictionary<int, int>(); // <thread id, LineSeries>
        private int x = 0;

        private void ResourceUsagePlot(object sender, EventArgs e)
        {
            CpuPlotView.InvalidatePlot(true);
        }
    }
}