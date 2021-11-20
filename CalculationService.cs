using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using OxyPlot;
using OxyPlot.Series;
using Timer = System.Timers.Timer;

namespace FourierTransas
{
    public class CalculationService : IDisposable
    {
        public List<PlotModel> PlotModels { get; private set; }
        private List<DataPoint>[] points;
        private int length;
        private Timer _timer;
        public IntPtr ThreadId { get; private set; }
        public double CounterValue { get; private set; }
        PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        Random r = new Random();

        public CalculationService()
        {
            Thread.BeginThreadAffinity();

            FFTModel[] models = new FFTModel[]
            {
                new(2000, 15),
                new(6000, 35),
                new(4400, 65)
            };
            PlotModels = models.Select(m => m.Plot).ToList();
            points = PlotModels.Select(m => (m.Series[0] as LineSeries).Points).ToArray();
            length = points[0].Count;
            
            _timer= new Timer(500);
            _timer.Elapsed += (obj, args) => UpdatePoints();
            _timer.Elapsed += (obj, args) => CheckCPULimit();
        }

        public void OnStart()
        {
            _timer.Enabled = true;
        }

        public void OnStop()
        {
            _timer.Enabled = false;
        }

        public void Dispose()
        {
            _timer.Enabled = false;
        }
        
        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();
        
        private void UpdatePoints()
        {
            var process = Process.GetCurrentProcess(); 
            var p1 = process.UserProcessorTime;
            var processThread = process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.UserProcessorTime;
            
            double[] gen = Generate.Sinusoidal(length, length * 2, r.Next(0, 199999), r.Next(0, 100));
            Complex[] complex = new Complex[length];
            for (int j = 0; j < length; j++) complex[j] = new Complex(gen[j], 0);

            Fourier.Forward(complex, FourierOptions.NoScaling);
            for (int j = 0; j < length; j++)
                gen[j] = Math.Sqrt(Math.Pow(complex[j].Real, 2) + Math.Pow(complex[j].Imaginary, 2)) * 2 / length;

            for (int i = 0; i < points.Length; i++)
            {
                lock (PlotModels[i].SyncRoot)
                {
                    for (int j = 0; j < length; j++)
                    {
                        points[i][j] = new DataPoint(points[i][j].X, points[i][j].Y + gen[j] * Math.Pow(-1, j + i));
                    }
                }
            }
            CounterValue = (processThread.UserProcessorTime - t1) / (process.UserProcessorTime - p1)/Environment.ProcessorCount;
        }

        private readonly int cpuLimit = 30;
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _timer.Interval = d;
                return CounterValue - cpuLimit;
            };
            _timer.Interval = MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 50, 600, 3, 5);
        }
    }
}