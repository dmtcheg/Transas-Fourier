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

        Process _process = Process.GetCurrentProcess();
        private System.Timers.Timer _timer;
        private Timer _limitTimer;
        private double _period;
        private int _limitTimerPeriod;
        public double CounterValue { get; private set; }
        private CpuCounterService _counterService;


        public CalculationService(CpuCounterService counterService)
        {
            _counterService = counterService;
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
            
            _timer = new System.Timers.Timer(100);
            _timer.Elapsed += (obj, e) => UpdatePoints();
            _timer.Enabled = true;

            _limitTimerPeriod = 5000;
            _limitTimer = new Timer(new TimerCallback(state => CheckCPULimit()), null, 5000, _limitTimerPeriod);
            
            processInitTime = _process.TotalProcessorTime;
        }

        public void OnStop()
        {
            _timer.Dispose();
            _limitTimer.Dispose();
        }

        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        private TimeSpan threadTime;
        private TimeSpan processInitTime;
        
        private void UpdatePoints()
        {
            Thread.BeginThreadAffinity();
            
            _process.Refresh();
            var processThread = _process.Threads.Cast<ProcessThread>().First(p => p.Id == GetCurrentThreadId());
            var t1 = processThread.TotalProcessorTime;

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
            threadTime += (processThread.TotalProcessorTime - t1);
            CounterValue = _counterService.Value * threadTime / (_process.TotalProcessorTime - processInitTime);
        }

        private double cpuLimit = 30;
        public double CpuLimit
        {
            get => cpuLimit;
            set => cpuLimit = value;
        }

        private double accuracy = 3;
        private bool isRootFinding = false;
        
        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _timer.Interval = d;
                Thread.Sleep((int)(10*d));
                return CounterValue - cpuLimit;
            };
            double interval;
            if (isRootFinding)
                return;
            isRootFinding = true;
            if (MathNet.Numerics.RootFinding.Bisection.TryFindRoot(f, 50, 800, accuracy, 5, out interval))
                _timer.Interval = interval;
            isRootFinding = false;
        }
    }
}