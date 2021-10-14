using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace FourierTransas
{
    public class FFTModel
    {
        public PlotModel Plot { get; private set; }

        public FFTModel()
        {
            Plot = new PlotModel { Title = "sample" };
            
            var len = 256; // freq
            var sampleRate = 2 * len;
            var h1 = Generate.Sinusoidal(len, sampleRate, 60, 10);
            var h2 = Generate.Sinusoidal(len, sampleRate, 120, 20);

            var samples = new Complex[len];
            for (int i = 0; i < len; i++)
                samples[i] = new Complex(h1[i] + h2[i], 0);

            // time -> freq
            Fourier.Forward(samples, FourierOptions.NoScaling);

            var magnitudes = new LineSeries();
            //var phases = new LineSeries();
            for (int i = 0; i < samples.Length/2; i++)
            {
                // амплитуда
                var magnitude = Math.Sqrt(Math.Pow(samples[i].Real, 2) + Math.Pow(samples[i].Imaginary, 2))*2/len;
                // фаза
                //var phase = Math.Atan2(samples[i].Imaginary, samples[i].Real);

                magnitudes.Points.Add(new DataPoint(2*i, magnitude));
                //phases.Points.Add(new DataPoint(2 * i, phase));
            }
            Plot.Series.Add(magnitudes);
            //Plot.Series.Add(phases);
        }
    }
}
