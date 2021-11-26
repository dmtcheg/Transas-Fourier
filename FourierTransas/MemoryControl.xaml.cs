using System;
using System.Windows.Controls;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    public partial class MemoryControl : UserControl, IDisposable
    {
        private DispatcherTimer _dTimer;

        public MemoryControl(Services.MonitorService service)
        {
            InitializeComponent();
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 200))};
            rc.RenderTarget = RenderTarget.Screen;

            RamPlotView.Model = service.RamModel;
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

        public void Dispose()
        {
        }
    }
}