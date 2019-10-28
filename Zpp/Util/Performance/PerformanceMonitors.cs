using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Zpp.Util.Performance
{
    public class PerformanceMonitors
    {
        private readonly Dictionary<InstanceToTrack, PerformanceMonitor> _monitors =
            new Dictionary<InstanceToTrack, PerformanceMonitor>();

        private readonly PerformanceMonitor _performanceMonitor =
            new PerformanceMonitor(InstanceToTrack.Global);

        public PerformanceMonitors()
        {
            foreach (InstanceToTrack instancesToTrack in Enum.GetValues(typeof(InstanceToTrack)))
            {
                _monitors.Add(instancesToTrack, new PerformanceMonitor(instancesToTrack));
            }
        }

        public void Start(InstanceToTrack instancesToTrack)
        {
            _monitors[instancesToTrack].Start();
        }

        public void Stop(InstanceToTrack instancesToTrack)
        {
            _monitors[instancesToTrack].Stop();
        }

        public void Start()
        {
            _performanceMonitor.Start();
        }

        public void Stop()
        {
            _performanceMonitor.Stop();
        }

        public override string ToString()
        {
            // create report
            string report = "---------------------------------------" + Environment.NewLine;

            foreach (InstanceToTrack instancesToTrack in Enum.GetValues(typeof(InstanceToTrack)))
            {
                report += _monitors[instancesToTrack].ToString() + Environment.NewLine +
                          Environment.NewLine;
            }

            report += _performanceMonitor.ToString() + Environment.NewLine + Environment.NewLine;
            // long currentMemoryUsage = GC.GetTotalMemory(false);
            long currentMemoryUsage = Process.GetCurrentProcess().WorkingSet64;
            report +=
                $"CurrentMemoryUsage: {DebuggingTools.Prettify(currentMemoryUsage)}" +
                Environment.NewLine;

            return report;
        }
    }
}