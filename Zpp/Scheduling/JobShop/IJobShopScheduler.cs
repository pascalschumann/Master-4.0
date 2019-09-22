namespace Zpp.Mrp.MachineManagement
{
    public interface IJobShopScheduler
    {
        /**
         * Giffler-Thomson
         */
        void JobSchedulingWithGifflerThompsonAsZaepfel(IPriorityRule priorityRule);
        
        
    }
}