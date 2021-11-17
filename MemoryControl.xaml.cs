using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using NickStrupat;
using OxyPlot;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    public partial class MemoryControl : UserControl
    {
        private DispatcherTimer _dTimer;
        private MonitorService _service;

        public MemoryControl(MonitorService service)
        {
            InitializeComponent();
            _service = service;
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 200))};
            rc.RenderTarget = RenderTarget.Screen;

            RamPlotView.Model = _service.RamModel;
            (RamPlotView.Model as IPlotModel).Render(rc, RamPlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _dTimer.Tick += (obj, e) => ResourceUsagePlot();
            _dTimer.IsEnabled = true;
        }

        private void ResourceUsagePlot()
        {
            RamPlotView.InvalidatePlot(true);
        }
    }
}