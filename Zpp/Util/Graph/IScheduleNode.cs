using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Util.Graph.impl;

namespace Zpp.Util.Graph
{
    public interface IScheduleNode
    {
        DueTime GetEndTime();

        DueTime GetStartTime();

        /**
         * Adapts the startTime and also adapts the endTime accordingly (if exists)
         */
        void SetStartTime(DueTime startTime);

        /**
         * Adapts the endTime and also adapts the startTime accordingly (if exists)
         */
        void SetEndTime(DueTime endTime);

        /**
         * Contains transition time if exits
         */
        Duration GetDuration();

        void SetDone();

        void SetInProgress();

        bool IsDone();

        NodeType GetNodeType();

        Id GetId();

        void ClearStartTime();

        void ClearEndTime();
    }
}