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
    public partial class ResourceControl : UserControl
    {
        private DispatcherTimer _dTimer;
        private MonitorService _monitorService;
        private CalculationService _calculationService;

        public ResourceControl(CalculationService cs)
        {
            _calculationService = cs;
            _monitorService = new MonitorService(cs);

            InitializeComponent();

            var monitorThread = new Thread(_monitorService.OnStart);
            monitorThread.Priority = ThreadPriority.AboveNormal;
            monitorThread.IsBackground = true;
            monitorThread.Start();
            
            MemControl.Content = new MemoryControl(_monitorService);

            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(400, 400))};
            rc.RenderTarget = RenderTarget.Screen;

            CpuPlotView.Model = _monitorService.ThreadModel;
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
            _monitorService?.Dispose();
        }

        private void PlotRender_LimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChartControl.CpuLimit = PlotSlider.Value;
        }
        private void Calc_LimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _calculationService.CpuLimit = CalcSlider.Value;
        }
        private void Monitor_LimitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _monitorService.CpuLimit = MonitorSlider.Value;
        }
    }
}