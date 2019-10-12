using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.Util.Graph
{
    public interface IScheduleNode: INode
    {
        DueTime GetEndTime();

        DueTime GetStartTime();

        /**
         * Adapts the startTime and also adapts the dueTime/endTime accordingly (if exists)
         */
        void SetStartTime(DueTime startTime);

        /**
         * Contains transition time if exits
         */
        Duration GetDuration();

        void SetDone();

        void SetInProgress();

        bool IsDone();
    }
}