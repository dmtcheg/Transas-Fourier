using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
    public partial class PerformanceControl : UserControl
    {
        private MonitorService _service;
        private DispatcherTimer _dTimer;
        private List<BarItem> items;

        /// <summary>
        /// потребление ресурсов процессора и оперативной памяти приложением
        /// </summary>
        public PerformanceControl(uint mainThreadId, CalculationService service)
        {
            InitializeComponent();

            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(300, 300))};
            rc.RenderTarget = RenderTarget.Screen;


            // Thread t = new Thread(() =>
            // {
            //     _service = new MonitorService(mainThreadId, service);
            //     _service.OnStart();
            // });
            // t.Priority = ThreadPriority.AboveNormal;
            // t.IsBackground = true;
            // t.Start();
            // t.Join();
            _service = new MonitorService();
            Task t = Task.Factory.StartNew(()=>
            {
                _service.OnStart(mainThreadId, service);
            }, TaskCreationOptions.LongRunning);
            
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
            _dTimer.Interval = TimeSpan.FromMilliseconds(300);
            _dTimer.Tick += (sender, args) => PerformanceBar();
            _dTimer.IsEnabled = true;
        }

        private void PerformanceBar()
        {
            items[0] = new BarItem(_service.CurrentMemoryLoad());
            items[1] = new BarItem(_service.CurrentCpuLoad());
            PerformancePlotView.InvalidatePlot(true);
        }

        private void PerformancePlotView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Thread resourceThread = new Thread(delegate()
            // {
                Window resourceWindow = new Window
                {
                    Title = "Использование ресурсов",
                    Content = new ResourceControl(_service)
                };
                resourceWindow.Show();
            //     Dispatcher.Run();
            // });
            // resourceThread.SetApartmentState(ApartmentState.STA);
            // resourceThread.IsBackground = true;
            // resourceThread.Start();
        }
    }
}