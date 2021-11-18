using System;
using System.Collections.Generic;
using System.Diagnostics;
using NickStrupat;
using OxyPlot;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using Timer = System.Timers.Timer;

namespace FourierTransas
{
    /// <summary>
    /// сервис для мониторинга и ограничения потребления ресурсов
    /// </summary>
    public class MonitorService : IDisposable
    {
        private Timer _timer;

        PerformanceCounter _cpuCounter =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);

        ComputerInfo _info = new ComputerInfo();
        Process _currentProc = Process.GetCurrentProcess();
        private IntPtr[] _threads;
        private uint _calcThreadId;
        private List<DataPoint> _cpuSamples;
        private List<DataPoint> _ramSamples;
        public PlotModel ThreadModel { get; private set; }
        public PlotModel RamModel { get; private set; }

        //todo: fix over 100% "load"

        public MonitorService()
        {
            ThreadModel = new PlotModel
            {
                Title = "CPU",
                IsLegendVisible = true,
                Series =
                {
                    new LineSeries() {Title = "Total CPU", Color = OxyColors.Green, Decimator = Decimator.Decimate}
                }
            };
            ThreadModel.Series.Add(new LineSeries()
                {Color = OxyColors.Orange, Title = "plot render", Decimator = Decimator.Decimate});
            ThreadModel.Series.Add(new LineSeries()
                {Color = OxyColors.Blue, Title = "recource monitor", Decimator = Decimator.Decimate});
            ThreadModel.Series.Add(new LineSeries()
                {Color = OxyColors.Brown, Title = "calc", Decimator = Decimator.Decimate});

            ThreadModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            _cpuSamples = (ThreadModel.Series[0] as LineSeries).Points;

            RamModel = new PlotModel()
            {
                Title = "Memory",
                IsLegendVisible = true,
                Series = {new LineSeries() {Title = "% RAM", Color = OxyColors.Red, Decimator = Decimator.Decimate}}
            };
            RamModel.Legends.Add(new Legend
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 12
            });
            _ramSamples = (RamModel.Series[0] as LineSeries).Points;
        }

        public void OnStart(IntPtr mainThreadId, IntPtr calcThreadId)
        {
            var processThreads = _currentProc.Threads
                .Cast<ProcessThread>()
                .ToArray();

            _threads = new IntPtr[3];
            // [1] не найден
            // вы полнился и "удалился/сменил id?"
            _threads[0] = mainThreadId;
            _threads[1] = GetCurrentThread();
            _threads[2] = calcThreadId;

            _timer = new Timer(1000);
            _timer.Elapsed += CpuRamUsage;

            //_timer.Elapsed += CheckCPULimit();
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

        public double CurrentMemoryLoad()
        {
            return 100 * Environment.WorkingSet / (long) _info.TotalPhysicalMemory;
        }

        public double CurrentCpuLoad()
        {
            return _cpuCounter.NextValue() / Environment.ProcessorCount;
        }

        private void CpuRamUsage(object sender, ElapsedEventArgs e)
        {
            _ramSamples.Add(new DataPoint(_ramSamples.Count + 1, CurrentMemoryLoad()));

            // var calcThread =_currentProc.Threads
            //     .Cast<ProcessThread>()
            //     .FirstOrDefault(p => p.Id == _calcThreadId);

            var x = _cpuSamples.Count;
            var load = CurrentCpuLoad();
            _cpuSamples.Add(new DataPoint(x, load));
            for (int i = 0; i < _threads.Length; i++)
            {
                QueryThreadCycleTime(_threads[i], out ulong t);
                var time = TimeSpan.FromTicks((long) t);

                var point = new DataPoint(x,
                    load * (time / _currentProc.UserProcessorTime));

                lock (ThreadModel.SyncRoot)
                {
                    (ThreadModel.Series[i] as LineSeries).Points.Add(point);
                }
            }
        }

        private readonly int cpuLimit = 30;

        private void CheckCPULimit()
        {
            Func<double, double> f = d =>
            {
                _timer.Interval = d;
                return CurrentCpuLoad() - cpuLimit;
            };
            _timer.Interval = MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 200, 800, 3, 3);
        }

        [DllImport("Kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("Kernel32.dll")]
        private static extern bool GetThreadTimes(IntPtr hThread,
            out FILETIME lpCreationTime, out FILETIME lpExitTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryThreadCycleTime(IntPtr ThreadHandle, out ulong CycleTime);
    }
}