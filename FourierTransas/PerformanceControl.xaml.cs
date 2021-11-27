using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    /// <summary>
    /// потребление ресурсов процессора и оперативной памяти приложением
    /// </summary>
    public partial class PerformanceControl : UserControl
    {
        private Services.MonitorService _monitor;
        //todo: naming
        private Services.CpuCounterService _counter;
        private DispatcherTimer _dTimer;
        private List<BarItem> items;

        public PerformanceControl(Services.CalculationService service, Services.CpuCounterService counterService)
        {
            InitializeComponent();

            _counter = counterService;
            _monitor = new Services.MonitorService(service, ChartControl.GetCounterValue, counterService);

            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(300, 300))};
            rc.RenderTarget = RenderTarget.Screen;

            var resourceModel = new PlotModel();
            var s = new BarSeries();
            s.Items.Add(new BarItem(0));
            s.Items.Add(new BarItem(0));
            resourceModel.Series.Add(s);
            items = s.Items;
            resourceModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "ResourceAxis",
                ItemsSource = new[] {"Mem", "CPU"}
            });
            PerformancePlotView.Model = resourceModel;
            (PerformancePlotView.Model as IPlotModel).Render(rc, PerformancePlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Render);
            _dTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _dTimer.Tick += (sender, args) => PerformanceBar();
            _dTimer.IsEnabled = true;
        }

        private void PerformanceBar()
        {
            items[0] = new BarItem(_counter.RamValue);
            items[1] = new BarItem(_counter.Value);
            PerformancePlotView.InvalidatePlot(true);
        }

        private void PerformancePlotView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Thread resourceThread = new Thread(delegate()
            {
                Window resourceWindow = new Window
                {
                    Title = "Использование ресурсов",
                    Content = new ResourceControl(_monitor),
                };
                resourceWindow.Closed += (o, args) => _monitor.OnStop();
                resourceWindow.Show();
                Dispatcher.Run();
            });
            resourceThread.SetApartmentState(ApartmentState.STA);
            resourceThread.IsBackground = true;
            resourceThread.Start();
        }
    }
}