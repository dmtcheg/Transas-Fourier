using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System.Windows;

namespace FourierTransas
{
    public partial class ResourceControl : UserControl, IDisposable
    {
        private DispatcherTimer _dTimer;
        private MonitorService _monitorService;

        public ResourceControl(MonitorService monitor)
        {
            _monitorService = monitor;

            InitializeComponent();

            var monitorThread = new Thread(monitor.OnStart);
            monitorThread.Priority = ThreadPriority.AboveNormal;
            monitorThread.IsBackground = true;
            monitorThread.Start();
            MemControl.Content = new MemoryControl(monitor);

            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 400))};
            rc.RenderTarget = RenderTarget.Screen;

            CpuPlotView.Model = monitor.ThreadModel;
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

        public void Dispose()
        {
        }

        private void PlotRender_LimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChartControl.CpuLimit = PlotSlider.Value;
        }
        private void Monitor_LimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _monitorService.CpuLimit = MonitorSlider.Value;
        }
        private void Calc_LimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _monitorService.CalculationService.CpuLimit = CalcSlider.Value;
        }
    }
}