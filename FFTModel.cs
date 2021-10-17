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
        public PlotModel Plot1 { get; private set; }
        public PlotModel Plot2 { get; private set; }
        public PlotModel Plot3 { get; private set; }
        public FFTModel()
        {
            Plot1 = new PlotModel { Title = "chart 1" };
            Plot2 = new PlotModel { Title = "chart 2" };
            Plot3 = new PlotModel { Title = "chart 3" };

            var len = 200000; // freq
            var sampleRate = 2 * len;
            var h1 = Generate.Sinusoidal(len, sampleRate, 60, 10);
            var h2 = Generate.Sinusoidal(len, sampleRate, 120, 20);

            var h3 = Generate.Sinusoidal(len, sampleRate, 600, 64);
            var h4 = Generate.Sinusoidal(len, sampleRate, 1200, 32);

            var h5 = Generate.Sinusoidal(len, sampleRate, 3000, 48);
            var h6 = Generate.Sinusoidal(len, sampleRate, 6000, 72);

            var samples = new Complex[3][];
            for (int i = 0; i < 3; i++)
            {
                samples[i] = new Complex[len];
            }
            for (int i = 0; i < len; i++)
            {
                samples[0][i] = new Complex(h1[i] + h2[i], 0);
                samples[1][i] = new Complex(h3[i] + h4[i], 0);
                samples[2][i] = new Complex(h5[i] + h6[i], 0);
            }

            // time -> freq
            for (int i = 0; i < 3; i++)
            {
                Fourier.Forward(samples[i], FourierOptions.NoScaling);
            }

            var magnitudes = new LineSeries[3];
            for (int i = 0; i < 3; i++)
            {
                magnitudes[i] = new LineSeries();
            }

            for (int i = 0; i < len / 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var magnitude = Math.Sqrt(Math.Pow(samples[j][i].Real, 2) + Math.Pow(samples[j][i].Imaginary, 2)) * 2 / len;
                    magnitudes[j].Points.Add(new DataPoint(2 * i, magnitude));
                }
                //var phase = Math.Atan2(samples[i].Imaginary, samples[i].Real
            }
            Plot1.Series.Add(magnitudes[0]);
            Plot2.Series.Add(magnitudes[1]);
            Plot3.Series.Add(magnitudes[2]);
        }
    }
}