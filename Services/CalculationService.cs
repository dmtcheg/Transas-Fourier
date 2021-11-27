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

namespace Services
{
    public class CalculationService : IService
    {
        public List<PlotModel> PlotModels { get; private set; }
        private List<DataPoint>[] points;
        private int length;
        Random r = new Random();

        private System.Timers.Timer _timer;
        private Timer _limitTimer;
        private double _period;
        private int _limitTimerPeriod;
        public double CounterValue { get; private set; }
        private CpuCounterService _counterService;


        public CalculationService(IService counterService)
        {
            _counterService = counterService as CpuCounterService;
            FFTModel[] models = new FFTModel[]
            {
                new(2000, 15),
                new(6000, 35),
                new(4400, 65)
            };
            PlotModels = models.Select(m => m.Plot).ToList();
            points = PlotModels.Select(m => (m.Series[0] as LineSeries).Points).ToArray();
            length = points[0].Count;
        }

        public void OnStart()
        {
            Thread.BeginThreadAffinity();
            // var callback = new TimerCallback((state) =>
            // {
            //     UpdatePoints();
            // });
            
            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += (obj, e) => UpdatePoints();
            _timer.Enabled = true;

            _limitTimer = new Timer(new TimerCallback((state) => CheckCPULimit()), null, 10000, 5000);
            _limitTimerPeriod = 5000;
        }

        public void OnStop()
        {
            _timer.Dispose();
            _limitTimer.Dispose();
        }

        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        private void UpdatePoints()
        {
            Thread.BeginThreadAffinity();
            
            var process = Process.GetCurrentProcess();
            var processThread = process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var p1 = process.UserProcessorTime;
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

            CounterValue = _counterService.Value *
                (processThread.UserProcessorTime - t1) / (process.UserProcessorTime - p1);
        }

        public double CpuLimit { get; set; } = 30;
        private double accuracy = 5;
        private bool isRootFinding = false;
        
        //todo: fix limitation
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _timer.Interval = d;
                return Math.Abs(CounterValue - CpuLimit);
            };
            
            if (Math.Abs(CounterValue - CpuLimit) > accuracy && !isRootFinding)
            {
                isRootFinding = true;
                try
                {
                    _timer.Interval = MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 100, 800, accuracy, 5);
                }
                catch
                {
                }

                isRootFinding = false;
            }
        }
    }
}