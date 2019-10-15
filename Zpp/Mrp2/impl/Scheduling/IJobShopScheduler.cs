using Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler;

namespace Zpp.Mrp2.impl.Scheduling
{
    public interface IJobShopScheduler
    {
        /**
         * Giffler-Thomson
         */
        void ScheduleWithGifflerThompsonAsZaepfel(IPriorityRule priorityRule);
        
        
    }
}