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
        }
        public void OnStop()
        {

        }
    }
}