using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FourierTransas
{
    public partial class ChartControl : UserControl
    {
        public ChartControl()
        {
            InitializeComponent();
            Chart0.DataContext = new FFTModel(3000, 64);
            Chart0.Model = (Chart0.DataContext as FFTModel).Plot;
            Chart1.DataContext = new FFTModel(60, 120);
            Chart1.Model = (Chart1.DataContext as FFTModel).Plot;
            Chart2.DataContext = new FFTModel(44100, 20);
            Chart2.Model = (Chart2.DataContext as FFTModel).Plot;
        }

        private bool update = false;
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            update = !update;
            var points = (Chart0.Model.Series[0] as LineSeries).Points;
            var len = points.Count;
            //var timer = new Timer(UpdateSeries, new object[] { Chart0, i++ }, 0, 1000);
            for (int i = 0; i <len && update; i++)
            {
                points[i] = new DataPoint(i, points[i].Y + 1);
                Chart0.Model.InvalidatePlot(true);
                Thread.Sleep(100);
            }
            //timer.Dispose();
        }

        private void UpdateSeries(object obj)
        {
            var info = obj as object[];
            var view = info[0] as OxyPlot.Wpf.PlotView;
            int i = (int)info[1];
            var points = (view.Model.Series[0] as LineSeries).Points;
            points[i] = new DataPoint(i, points[i].Y + 1);
            view.InvalidatePlot(true);
        }
    }
}
