using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
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
        private ComputerInfo _info = new ComputerInfo();
        private MonitorService _service;

        public MemoryControl(MonitorService service)
        {
            InitializeComponent();
            _service = service;
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 200))};
            rc.RenderTarget = RenderTarget.Screen;

            RamPlotView.Model = _service.RamModel;
            (RamPlotView.Model as IPlotModel).Render(rc, RamPlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Render);
            _dTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dTimer.Tick += (obj, e) => ResourceUsagePlot();
            _dTimer.Start();
        }

        private void ResourceUsagePlot()
        {
            lock (RamPlotView.Model.SyncRoot)
            {
                RamPlotView.InvalidatePlot(true);
            }
        }
    }
}