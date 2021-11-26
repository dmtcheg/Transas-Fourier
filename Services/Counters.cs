using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Services
{
    public class Counters
    {
        public static void CheckCPULimit(DispatcherTimer timer, Func<double> counterValue, int cpuLimit)
        {
            Func<double, double> f = d =>
            {
                timer.Interval = TimeSpan.FromMilliseconds(d);
                return counterValue() - cpuLimit;
            };
            timer.Interval = TimeSpan.FromMilliseconds(MathNet.Numerics.RootFinding.Bisection.FindRoot(f, 100, 600, 3, 4));
        }

        public static double GetThreadCpuUsage(ProcessThread thread)
        {
            return 0;
        }
    }
}