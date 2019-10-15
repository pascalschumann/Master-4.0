using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.ZppSimulator.impl
{
    public class SimulationInterval
    {
        public SimulationInterval(long startAt, long interval)
        {
            StartAt = startAt;
            Interval = interval;
        }
        public long StartAt { get; }
        public long Interval { get;  }
        public long EndAt => StartAt + Interval;
        
        public bool IsWithinInterval(DueTime dueTime)
        {
            bool isInInterval = dueTime.GetValue() <= EndAt &&
                                dueTime.GetValue() >= StartAt;
            return isInInterval;
        }
    }
    
    
}
