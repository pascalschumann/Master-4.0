namespace Zpp.Scheduling
{
    public interface IBackwardsScheduler
    {
        void ScheduleBackward(bool clearOldTimes);
    }
}