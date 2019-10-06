namespace Zpp.Scheduling.impl.JobShop
{
    public interface IJobShopScheduler
    {
        /**
         * Giffler-Thomson
         */
        void ScheduleWithGifflerThompsonAsZaepfel(IPriorityRule priorityRule);
        
        
    }
}