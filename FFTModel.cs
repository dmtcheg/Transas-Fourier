using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using System.Numerics;
using MathNet.Numerics;

namespace FourierTransas
{
    public class FFTModel
    {
        public PlotModel Plot { get; private set; }
        public FFTModel()
        {
            Plot = new PlotModel { Title = "sample" };
            Func<double, double> signal = k => Math.Sin(10 * 2 * Math.PI * k) + 0.5 * Math.Sin(5 * 2 * Math.PI * k);
            
            double[] x = Generate.LinearRangeMap(0, (double)1 / 2000, 1, signal);
            var x_fourier = x.Select(d => new Complex(d, 0)).ToArray();
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(x_fourier);
            var orig = new LineSeries();
            var transform = new LineSeries();
            //todo: create correct datapoint
            for (var i = 0; i < 2000; i++)
            {
                orig.Points.Add(new DataPoint(i * 0.0005, x[i]));
                transform.Points.Add(new DataPoint(i*0.0005, x_fourier[i].Real));
            }
            Plot.Series.Add(orig);
            Plot.Series.Add(transform);
        }
    }
}
