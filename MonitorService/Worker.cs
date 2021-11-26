using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OxyPlot;

namespace MonitorService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Timer _timer;
        private PerformanceCounter _counter;
        //todo: use linked list?
        private List<double> samples = new List<double>();
        
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(Count, new AutoResetEvent(true), 100, 80);
            return Task.CompletedTask;
        }

        private int limit;
        public void Count(object obj)
        {
            if (samples.Count < limit)
                samples.Add(_counter.NextValue()/Environment.ProcessorCount);
            else
            {
                samples.RemoveAt(0);
                samples.Add(_counter.NextValue());
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }
    }
}