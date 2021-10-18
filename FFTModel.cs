using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using OxyPlot;
using OxyPlot.Series;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Runtime.CompilerServices;

namespace FourierTransas
{
    public class FFTModel : INotifyPropertyChanged
    {
        private PlotModel plot;
        public PlotModel Plot
        {
            get => plot;
            set
            {
                plot = value;
                OnPropertyChanged();
            }
        }

        public FFTModel(double frequency, double amplitude)
        {
            Plot = new PlotModel { Title = "chart" };
            var len = 50000; // freq
            var sampleRate = 4 * len;
            var wave = Generate.Sinusoidal(len, sampleRate, frequency, amplitude);
            var samples = new Complex[len];
            for (int i = 0; i < len; i++)
            {
                samples[i] = new Complex(wave[i], 0);
            }
            // time -> freq
            Fourier.Forward(samples, FourierOptions.NoScaling);
            var magnitudes = new LineSeries();
            for (int i = 0; i < len / 2; i++)
            {
                //амплитуда
                var magnitude = Math.Sqrt(Math.Pow(samples[i].Real, 2) + Math.Pow(samples[i].Imaginary, 2)) * 2 / len;
                magnitudes.Points.Add(new DataPoint(2 * i, magnitude));
            }
            Plot.Series.Add(magnitudes);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}