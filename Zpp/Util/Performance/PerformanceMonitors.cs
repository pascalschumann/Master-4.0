using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Zpp.Util.Performance
{
    public class PerformanceMonitors
    {
        private readonly Dictionary<InstanceToTrack, PerformanceMonitor> _monitors =
            new Dictionary<InstanceToTrack, PerformanceMonitor>();

        public PerformanceMonitors()
        {
            
            foreach (InstanceToTrack instanceToTrack in Enum.GetValues(typeof(InstanceToTrack)))
            {
                    _monitors.Add(instanceToTrack, new PerformanceMonitor(instanceToTrack));   
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
            _monitors[InstanceToTrack.Global].Start();
        }

        public void Stop()
        {
            _monitors[InstanceToTrack.Global].Stop();
        }
        
        // replaces ToString() since debugger is unusable
        public string AsString()
        {
            // create report
            string report = "---------------------------------------" + Environment.NewLine;

            foreach (InstanceToTrack instancesToTrack in Enum.GetValues(typeof(InstanceToTrack)))
            {
                report += _monitors[instancesToTrack].AsString() + Environment.NewLine +
                          Environment.NewLine;
            }
            
            // long currentMemoryUsage = GC.GetTotalMemory(false);
            long currentMemoryUsage = Process.GetCurrentProcess().WorkingSet64;
            report +=
                $"CurrentMemoryUsage: {DebuggingTools.Prettify(currentMemoryUsage)}" +
                Environment.NewLine;

            return report;
        }
    }
}