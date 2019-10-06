using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph;

namespace Zpp.DataLayer
{
    public interface IDemandOrProvider: INode
    {
        /**
         * For a demand this is usually the startTime, for a provider it's usually the endTime
         */
        DueTime GetDueTime();

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

        Quantity GetQuantity();

        void SetDone();

        void SetInProgress();

        bool IsDone();
    }
}