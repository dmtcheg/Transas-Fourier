using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NickStrupat;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;

namespace FourierTransas
{
    public partial class PerformanceControl : UserControl
    {
        PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        private DispatcherTimer _dTimer;
        private ComputerInfo info = new ComputerInfo();
        private List<BarItem> items;
        
        public PerformanceControl()
        {
            InitializeComponent();
            SkiaRenderContext rc = new SkiaRenderContext() {SkCanvas = new SKCanvas(new SKBitmap(300, 300))};
            rc.RenderTarget = RenderTarget.Screen;
    
            var resourceModel = new PlotModel();
            var s = new BarSeries();
            s.Items.Add(new BarItem(100*Environment.WorkingSet/(long)info.TotalPhysicalMemory));
            s.Items.Add(new BarItem(_cpuCounter.NextValue()/Environment.ProcessorCount));
            resourceModel.Series.Add(s);
            items = s.Items;
            resourceModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "ResourceAxis",
                ItemsSource = new[]{"Mem", "CPU"}
            });
            PerformancePlotView.Model = resourceModel;
            (PerformancePlotView.Model as IPlotModel).Render(rc, PerformancePlotView.Model.PlotArea);

            _dTimer = new DispatcherTimer(DispatcherPriority.Render);
            _dTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dTimer.Tick += PerformanceBar;
            _dTimer.Start();
        }

        private void PerformanceBar(object sender, EventArgs e)
        {
            items[0] = new BarItem(100*Environment.WorkingSet/(long)info.TotalPhysicalMemory);
            var cpuLoad = _cpuCounter.NextValue() / Environment.ProcessorCount;
            if (cpuLoad > 1) items[1] = new BarItem(cpuLoad);
            PerformancePlotView.InvalidatePlot(true);
        }

        private void PerformancePlotView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Thread resourceThread = new Thread(delegate()
            {
                Window resourceWindow = new Window
                {
                    Title = "Использование ресурсов",
                    Content = new ResourceControl(_cpuCounter)
                };
                resourceWindow.Show();
                Dispatcher.Run();
            });
            resourceThread.SetApartmentState(ApartmentState.STA);
            resourceThread.IsBackground = true;
            resourceThread.Start();
        }
    }
}