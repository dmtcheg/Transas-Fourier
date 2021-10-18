using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FourierTransas
{
    class PlotService
    {
        private OxyPlot.PlotModel Plot { get; set; }

        public PlotService(PlotModel plot)
        {
            Plot = plot;
        }

        public void OnStart()
        {
            //todo: change 1 point and update plot?
            //while (true)
            //{
            //    var series = Plot.Series[0] as LineSeries;
            //    series.Points.RemoveAt(0);
            //    series.Points.Add(new OxyPlot.DataPoint());
            //}
        }
        public void OnStop()
        {

        }
    }
}