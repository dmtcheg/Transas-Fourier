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

        public ResourceControl(MonitorService service, IntPtr mainThreadId, IntPtr calcThreadId)
        {
            InitializeComponent();
            
            _service = service;
            MemControl.Content = new MemoryControl(service);

            var monitorThread = new Thread(delegate()
            {
                _service.OnStart(mainThreadId, calcThreadId);
            });
            monitorThread.Priority = ThreadPriority.AboveNormal;
            monitorThread.IsBackground = true;
            
            monitorThread.Start();
            
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 400))};
            rc.RenderTarget = RenderTarget.Screen;

            CpuPlotView.Model = _service.ThreadModel;
            (CpuPlotView.Model as IPlotModel).Render(rc, CpuPlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _dTimer.Tick += ResourceUsagePlot;
            _dTimer.IsEnabled = true;
        }

        private void ResourceUsagePlot(object sender, EventArgs e)
        {
            CpuPlotView.InvalidatePlot(true);
        }
    }
}