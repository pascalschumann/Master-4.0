namespace Zpp.Mrp.MachineManagement
{
    public interface IJobShopScheduler
    {
        /**
         * Giffler-Thomson
         */
        void ScheduleWithGifflerThompsonAsZaepfel(IPriorityRule priorityRule);
        
        
    }
}