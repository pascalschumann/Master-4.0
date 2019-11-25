using System;
using System.Diagnostics;

namespace Zpp.Util.Performance
{
    public class PerformanceMonitor
    {
        private readonly InstanceToTrack _instanceToTrack;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _isStarted = false;

        public PerformanceMonitor(InstanceToTrack instanceToTrack)
        {
            _instanceToTrack = instanceToTrack;
        }

        public bool IsStarted()
        {
            return _isStarted;
        }

        public void Start()
        {
            if (_isStarted)
            {
                throw new MrpRunException(
                    "A PerformanceMonitor cannot be started before it is stopped.");
            }

            _stopwatch.Start();
            _isStarted = true;
        }

        public void Stop()
        {
            if (_isStarted == false)
            {
                throw new MrpRunException(
                    "A PerformanceMonitor cannot be stopped before it is started.");
            }
            _stopwatch.Stop();
            _isStarted = false;
        }

        public override string ToString()
        {
            string instanceToTrack = Enum.GetName(typeof(InstanceToTrack), _instanceToTrack);
            string objectAsString =
                $"\"{instanceToTrack}\": \"{_stopwatch.Elapsed.Ticks}\",";
            return objectAsString;
        }

        public override bool Equals(object obj)
        {
            PerformanceMonitor other = (PerformanceMonitor) obj;
            return _instanceToTrack.Equals(other._instanceToTrack);
        }

        public override int GetHashCode()
        {
            return _instanceToTrack.GetHashCode();
        }
    }
}